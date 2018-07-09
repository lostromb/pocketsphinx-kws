using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.CPlusPlus
{
    /// <summary>
    /// Wrapper for C-style FILE read functionality
    /// </summary>
    public class FILE
    {
        public const int SEEK_SET = 0; // Beginning of file
        public const int SEEK_CUR = 1; // Current position of the file pointer
        public const int SEEK_END = 2; // End of file
        public const int EOF = -1;

        private FileStream _stream;

        private FILE(string filename, string accessMode)
        {
            _stream = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite);
        }

        public static FILE fopen(Pointer<byte> fileName, string accessMode)
        {
            return new FILE(cstring.FromCString(fileName), accessMode);
        }

        public static bool file_exists(Pointer<byte> fileName)
        {
            return new FileInfo(cstring.FromCString(fileName)).Exists;
        }

        public void fclose()
        {
            _stream.Close();
        }

        /// <summary>
        /// Sets the position indicator associated with the stream to a new position.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns>If successful, the function returns zero.
        /// Otherwise, it returns non-zero value.
        /// If a read or write error occurs, the error indicator (ferror) is set.</returns>
        public int fseek(long offset, int origin)
        {
            if (origin == SEEK_SET)
            {
                _stream.Seek(offset, SeekOrigin.Begin);
            }
            else if (origin == SEEK_CUR)
            {
                _stream.Seek(offset, SeekOrigin.Current);
            }
            else if (origin == SEEK_END)
            {
                _stream.Seek(offset, SeekOrigin.End);
            }

            return 0;
        }

        /// <summary>
        /// Returns the current value of the position indicator of the stream.
        /// For binary streams, this is the number of bytes from the beginning of the file.
        /// </summary>
        /// <returns></returns>
        public long ftell()
        {
            return _stream.Position;
        }

        public int fgetc()
        {
            int returnVal = _stream.ReadByte();
            if (returnVal < 0)
            {
                return EOF;
            }
            
            return returnVal;
        }

        public int fwrite(object val, int size, int count)
        {
            throw new NotImplementedException();
        }
        
        public int ferror()
        {
            // Not implemented
            return 0;
        }

        /// <summary>
        /// Reads an array of <i>count</i> elements, each one with a size of <i>size</i> bytes, from the stream and stores them in the block of memory specified by <i>ptr</i>.
        /// The position indicator of the stream is advanced by the total amount of bytes read.
        /// The total amount of bytes read if successful is (size * count).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ptr"></param>
        /// <param name="size"></param>
        /// <param name="count"></param>
        /// <returns>The total number of elements successfully read is returned. 
        /// If this number differs from the count parameter, either a reading error occurred or the end-of-file was reached while reading.
        /// In both cases, the proper indicator is set, which can be checked with ferror and feof, respectively.
        /// If either size or count is zero, the function returns zero and both the stream state and the content pointed by ptr remain unchanged.</returns>
        public uint fread(Pointer<byte> ptr, uint size, uint count)
        {
            if (size == 0 || count == 0)
            {
                return 0;
            }
            
            byte[] block = new byte[size * count];
            int bytesRead = 0;
            while (bytesRead < count)
            {
                int readCount = _stream.Read(block, bytesRead, block.Length - bytesRead);
                bytesRead += readCount;
                if (readCount == 0)
                {
                    // end of stream
                    break;
                }
            }

            ptr.MemCopyFrom(block, 0, bytesRead);
            return (uint)bytesRead / size;
        }

        /// <summary>
        /// Reads characters from stream and stores them as a C string into str until (num-1) characters have been read or either a newline or the end-of-file is reached, whichever happens first.
        /// A newline character makes fgets stop reading, but it is considered a valid character by the function and included in the string copied to str.
        /// A terminating null character is automatically appended after the characters copied to str.
        /// Notice that fgets is quite different from gets: not only fgets accepts a stream argument, but also allows to specify the maximum size of str and includes in the string any ending newline character.
        /// </summary>
        /// <param name="str">Pointer to an array of chars where the string read is copied.</param>
        /// <param name="num">Maximum number of characters to be copied into str (including the terminating null-character).</param>
        /// <returns>On success, the function returns str.
        /// If the end-of-file is encountered while attempting to read a character, the eof indicator is set(feof). If this happens before any characters could be read, the pointer returned is a null pointer (and the contents of str remain unchanged).
        /// If a read error occurs, the error indicator (ferror) is set and a null pointer is also returned (but the contents pointed by str may have changed).</returns>
        public Pointer<byte> fgets(Pointer<byte> str, int num)
        {
            // FIXME NOT TESTED
            int charsRead = 0;
            while (charsRead < num - 1)
            {
                int c = fgetc();
                if (c == EOF)
                {
                    if (charsRead == 0)
                    {
                        return PointerHelpers.NULL<byte>();
                    }

                    str[charsRead] = 0;
                    return str;
                }

                str[charsRead++] = (byte)c;
                if (c == '\n')
                {
                    str[charsRead] = 0;
                    return str;
                }
            }

            str[charsRead] = 0;
            return str;
        }

        /// <summary>
        /// Implements fscanf("%d")
        /// Any number of decimal digits (0-9), optionally preceded by a sign (+ or -).
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public int fscanf_d(out int d)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implements fscanf("%f ")
        /// A series of decimal digits, optionally containing a decimal point, optionally preceeded by a sign (+ or -) and optionally followed by the e or E character and a decimal integer (or some of the other sequences supported by strtod).
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public int fscanf_f_(out float d)
        {
            // Remember that the whitespace captures all available whitspace, including none, so treat it as the regex \s*
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get stats on a file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="returnVal"></param>
        public static int stat(Pointer<byte> fileName, BoxedValue<stat_t> returnVal)
        {
            string resolvedFileName = cstring.FromCString(fileName);
            System.IO.FileInfo file = new System.IO.FileInfo(resolvedFileName);
            returnVal.Val = new stat_t();
            if (file.Exists)
            {
                returnVal.Val.st_mtime = file.LastWriteTime.ToFileTime();
                returnVal.Val.st_size = file.Length;
                return 0;
            }
            else
            {
                return -1; // File not found, return EBADF for ferr
            }
        }
    }
}
