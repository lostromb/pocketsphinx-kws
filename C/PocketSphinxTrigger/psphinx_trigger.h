#pragma once

#ifdef __cplusplus
extern "C" {
#endif

__declspec(dllexport) void* trigger_create(char* modelDir, char* dictionaryFile, bool verboseLogging);

__declspec(dllexport) int trigger_reconfigure(void* decoder, char* keywordFile);

__declspec(dllexport) int trigger_start_processing(void* decoder);

__declspec(dllexport) int trigger_stop_processing(void* decoder);

__declspec(dllexport) bool trigger_process_samples(void* decoder, short* samples, int numSamples);

__declspec(dllexport) void trigger_get_last_hyp(void* decoder, char* buffer);

__declspec(dllexport) int trigger_free(void* decoder);

#ifdef __cplusplus
}
#endif