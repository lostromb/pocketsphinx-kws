#include "pocketsphinx.h"
#include "ps_search.h"
#include "psphinx_trigger.h"
#include "kws_search.h"

#include "err.h"
#include "ckd_alloc.h"
#include "strfuncs.h"
#include "pio.h"
#include "cmd_ln.h"

#include <string>

struct trigger_adapter
{
	ps_decoder_t* ps;
	bool utt_started;
	bool user_is_speaking;
	bool triggered;
	char* last_hyp;
	kws_search_t* kwss;
};

void* trigger_create(char* modelDir, char* dictionaryFile, bool verboseLogging)
{
	//printf("            creating recognizer\n");
	
	ps_decoder_t* ps = NULL;
	cmd_ln_t* config = NULL;

	if (verboseLogging)
	{
		config = cmd_ln_init(NULL, ps_args(), true,
			"-hmm", modelDir,
			"-dict", dictionaryFile,
			"-verbose", "y",
			NULL);
	}
	else
	{
		config = cmd_ln_init(NULL, ps_args(), true,
			"-hmm", modelDir,
			"-dict", dictionaryFile,
			"-logfn", "NUL",
			NULL);
	}
	
	ps = ps_init(config);

	cmd_ln_free_r(config);
	
	trigger_adapter* adapter = new trigger_adapter();
	adapter->ps = ps;
	adapter->user_is_speaking = false;
	adapter->last_hyp = new char[512];
	adapter->last_hyp[0] = 0;

	return adapter;
}

int trigger_reconfigure(void* decoder, char* keyfile)
{
	trigger_adapter* adapter = (trigger_adapter*)decoder;
	ps_decoder_t* ps = adapter->ps;

	//printf("            reconfiguring %s\n", keyfile);
	
	if (ps_set_kws(ps, "keyword_search", keyfile) != 0)
	{
		return -1;
	}

	if (ps_set_search(ps, "keyword_search") != 0)
	{
		return -1;
	}

	return 0;
}

int trigger_start_processing(void* decoder)
{
	//printf("            process start\n");
	
	trigger_adapter* adapter = (trigger_adapter*)decoder;
	ps_decoder_t* ps = adapter->ps;

	adapter->utt_started = true;
	return ps_start_utt(ps); // todo use ps_start_stream?
}

int trigger_stop_processing(void* decoder)
{
	//printf("            process stop\n");
	
	trigger_adapter* adapter = (trigger_adapter*)decoder;
	ps_decoder_t* ps = adapter->ps;

	if (adapter->utt_started)
	{
		ps_end_utt(ps);
		adapter->utt_started = false;
		if (adapter->last_hyp)
		{
			adapter->last_hyp[0] = '\0';
		}
	}

	return 0;
}

bool trigger_process_samples(void* decoder, short* samples, int numSamples)
{
	trigger_adapter* adapter = (trigger_adapter*)decoder;
	ps_decoder_t* ps = adapter->ps;

	ps_process_raw(ps, samples, numSamples, false, false);
	uint8 in_speech = ps_get_in_speech(ps);
	if (in_speech && !adapter->user_is_speaking)
	{
		adapter->user_is_speaking = true;
	}

	bool returnVal = false;

	int score;
	const char* hyp = ps_get_hyp(ps, &score);

	if (hyp)
	{
		//printf("            tenative hyp %s\n", hyp);
		if (!adapter->triggered)
		{
			returnVal = true;
			adapter->triggered = true;
			size_t hypsize = strnlen(hyp, 500);
			strncpy(adapter->last_hyp, hyp, hypsize);
			adapter->last_hyp[hypsize] = 0;
			//printf("            adapter last hyp is %s\n", hyp);
		}
	}

	if (!in_speech && adapter->user_is_speaking)
	{
		/* speech -> silence transition, time to start new utterance  */
		ps_end_utt(ps);
		adapter->utt_started = false;
		
		hyp = ps_get_hyp(ps, &score);

		if (hyp)
		{
			//printf("            final hyp %s\n", hyp);
			if (!adapter->triggered)
			{
				returnVal = true;
				adapter->triggered = true;
				size_t hypsize = strnlen(hyp, 500);
				strncpy(adapter->last_hyp, hyp, hypsize);
				adapter->last_hyp[hypsize] = 0;
				//printf("            adapter last hyp is %s\n", hyp);
			}
		}

		if (ps_start_utt(ps) < 0)
		{
			//printf("            failed to restart utterance\n");
		}
		adapter->utt_started = true;

		adapter->user_is_speaking = false;
		adapter->triggered = false;
		//printf("Ready....\n");
	}

	return returnVal;
}

void trigger_get_last_hyp(void* decoder, char* buffer)
{
	trigger_adapter* adapter = (trigger_adapter*)decoder;
	memcpy(buffer, adapter->last_hyp, 512);
}

int trigger_free(void* decoder)
{
	trigger_adapter* adapter = (trigger_adapter*)decoder;
	ps_free(adapter->ps);
	free(adapter->last_hyp);
	free(adapter);
	return 0;
}