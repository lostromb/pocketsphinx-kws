using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.CPlusPlus
{
    public static class cstring
    {
        /// <summary>
        /// Returns the length of the C string str.
        /// The length of a C string is determined by the terminating null-character: A C string is as long as the number of characters
        /// between the beginning of the string and the terminating null character (without including the terminating null character itself).
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static uint strlen(Pointer<byte> str)
        {
            uint returnVal;
            for (returnVal = 0; str[returnVal] != 0; returnVal++) { }
            return returnVal;
        }

        /// <summary>
        /// Compares the C string str1 to the C string str2.
        /// This function starts comparing the first character of each string. If they are equal to each other, it continues
        /// with the following pairs until the characters differ or until a terminating null-character is reached.
        /// This function performs a binary comparison of the characters.
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <returns>Returns an integral value indicating the relationship between the strings:
        /// negative .Deref. the first character that does not match has a lower value in ptr1 than in ptr2
        /// zero .Deref. the strings are equal
        /// positive .Deref. the first character that does not match has a greater value in ptr1 than in ptr2</returns>
        public static int strcmp(Pointer<byte> one, Pointer<byte> two)
        {
            return strncmp(one, two, uint.MaxValue);
        }

        /// <summary>
        /// Compares the C string str1 to the C string str2.
        /// This function starts comparing the first character of each string. If they are equal to each other, it continues
        /// with the following pairs until the characters differ or until a terminating null-character is reached.
        /// This function performs a binary comparison of the characters.
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <param name="n">The maximum number of characters to compare</param>
        /// <returns>Returns an integral value indicating the relationship between the strings:
        /// negative .Deref. the first character that does not match has a lower value in ptr1 than in ptr2
        /// zero .Deref. the strings are equal
        /// positive .Deref. the first character that does not match has a greater value in ptr1 than in ptr2</returns>
        public static int strncmp(Pointer<byte> one, Pointer<byte> two, uint n)
        {
            for (uint c = 0; c < n; c++)
            {
                if (one[c] == 0)
                {
                    if (two[c] == 0)
                    {
                        return 0;
                    }
                    else
                        return -1;
                }
                else if (two[c] == 0)
                {
                    if (one[c] == 0)
                    {
                        return 0;
                    }
                    else
                        return 1;
                }
                else if (one[c] != two[c])
                {
                    return Math.Sign(one[c] - two[c]);
                }
            }

            return 0;
        }

        /// <summary>
        /// A sequence of calls to this function split str into tokens, which are sequences of contiguous characters
        /// separated by any of the characters that are part of delimiters.
        /// 
        /// On a first call, the function expects a C string as argument for str, whose first character is used as the
        /// starting location to scan for tokens. In subsequent calls, the function expects a null pointer and uses the
        /// position right after the end of the last token as the new starting location for scanning.
        /// 
        /// To determine the beginning and the end of a token, the function first scans from the starting location for
        /// the first character not contained in delimiters(which becomes the beginning of the token). And then scans
        /// starting from this beginning of the token for the first character contained in delimiters, which becomes the end of the token.
        /// The scan also stops if the terminating null character is found.
        /// 
        /// This end of the token is automatically replaced by a null-character, and the beginning of the token is returned by the function.
        /// Once the terminating null character of str is found in a call to strtok, all subsequent calls to this
        /// function (with a null pointer as the first argument) return a null pointer.
        /// 
        /// The point where the last token was found is kept internally by the function to be used on the next call
        /// (particular library implementations are not required to avoid data races).
        /// </summary>
        /// <param name="str">C string to truncate.
        /// Notice that this string is modified by being broken into smaller strings(tokens).
        /// Alternativelly, a null pointer may be specified, in which case the function
        /// continues scanning where a previous successful call to the function ended.</param>
        /// <param name="delimiters"></param>
        /// <returns>C string containing the delimiter characters.
        /// These can be different from one call to another.</returns>
        public static Pointer<byte> strtok(Pointer<byte> str, Pointer<byte> delimiters)
        {
            if (str.IsNull)
            {
                str = _last_strtok;

                if (str.IsNull)
                {
                    // No more tokens to find
                    return PointerHelpers.NULL<byte>();
                }
            }
            
            // Find the start position
            int start;
            int end;
            for (start = 0; IsInSet(str[start], delimiters) && str[start] != 0; start++) { }

            // Reached end of string while looking for start
            if (str[start] == 0)
            {
                _last_strtok = PointerHelpers.NULL<byte>();
            }

            // Now look for the end of the token
            for (end = start + 1; !IsInSet(str[end], delimiters) && str[end] != 0; end++) { }

            // Reached end of string
            if (str[end] == 0)
            {
                _last_strtok = PointerHelpers.NULL<byte>();
            }
            else
            {
                // Insert null token at the end
                str[end] = 0;

                // Set state for next search
                _last_strtok = str.Point(end + 1);
            }

            return str.Point(start);
        }

        private static bool IsInSet(byte val, Pointer<byte> set)
        {
            for (int c = 0; set[c] != 0; c++)
            {
                if (val == set[c])
                {
                    return true;
                }
            }

            return false;
        }

        private static Pointer<byte> _last_strtok = PointerHelpers.NULL<byte>();

        /// <summary>
        /// Returns a pointer to the first occurrence of character in the C string str.
        /// The terminating null-character is considered part of the C string. Therefore, it can also be located in order to retrieve a pointer to the end of a string.
        /// </summary>
        /// <param name="str">A C string</param>
        /// <param name="character">Character to be located</param>
        /// <returns>A pointer to the first occurrence of character in str.
        ///If the character is not found, the function returns a null pointer.</returns>
        public static Pointer<byte> strchr(Pointer<byte> str, byte character)
        {
            uint c = 0;
            while (true)
            {
                if (str[c] == character)
                {
                    return str.Point(c);
                }
                if (str[c] == 0)
                {
                    return PointerHelpers.NULL<byte>();
                }

                c++;
            }
        }

        public static Pointer<byte> strrchr(Pointer<byte> str, byte character)
        {
            for (int c = (int)(strlen(str) - 1); c >= 0; c--)
            {
                if (str[c] == character)
                {
                    return str.Point(c);
                }
            }

            return PointerHelpers.NULL<byte>();
        }

        /// <summary>
        /// Returns the length of the initial portion of str1 which consists only of characters that are part of str2.
        /// The search does not include the terminating null-characters of either strings, but ends there.
        /// </summary>
        /// <param name="one">C string to be scanned.</param>
        /// <param name="two">C string containing the characters to match.</param>
        /// <returns>The length of the initial portion of str1 containing only characters that appear in str2.
        /// Therefore, if all of the characters in str1 are in str2, the function returns the length of the entire str1 string, and if the first character in str1 is not in str2, the function returns zero.
        /// size_t is an unsigned integral type.</returns>
        public static uint strspn(Pointer<byte> one, Pointer<byte> two)
        {
            uint c = 0;
            uint tlen = strlen(two);
            while (true)
            {
                byte ch = one[c];
                if (ch == 0)
                {
                    // Reached null terminator, return the length of the entire string
                    return c;
                }
                
                for (uint z = 0; z < tlen; z++)
                {
                    if (ch == two[z])
                    {
                        // Character was in set; ok to continue to next char
                        c++;
                        continue;
                    }
                }

                // Character was not in set, return the current length up to this point
                return c;
            }
        }

        /// <summary>
        /// Copies the C string pointed by source into the array pointed by destination,
        /// including the terminating null character (and stopping at that point).
        /// 
        /// To avoid overflows, the size of the array pointed by destination shall be long enough to
        /// contain the same C string as source (including the terminating null character), and should not overlap in memory with source.
        /// </summary>
        /// <param name="dest">Pointer to the destination array where the content is to be copied.</param>
        /// <param name="source">C string to be copied.</param>
        /// <returns>destination is returned</returns>
        public static Pointer<byte> strcpy(Pointer<byte> dest, Pointer<byte> source)
        {
            uint c;
            for (c = 0; source[c] != 0; c++)
            {
                dest[c] = source[c];
            }

            // Copy the null terminator
            dest[c] = 0;

            return dest;
        }

        /// <summary>
        /// Copies the first num characters of source to destination. If the end of the source C string (which is signaled by a null-character) is
        /// found before num characters have been copied, destination is padded with zeros until a total of num characters have been written to it.
        /// 
        /// No null-character is implicitly appended at the end of destination if source is longer than num. Thus, in this case, destination shall
        /// not be considered a null terminated C string (reading it as such would overflow).
        /// 
        /// destination and source shall not overlap (see memmove for a safer alternative when overlapping).
        /// </summary>
        /// <param name="dest">Pointer to the destination array where the content is to be copied.</param>
        /// <param name="source">C string to be copied.</param>
        /// <param name="n">Maximum number of characters to be copied from source.</param>
        /// <returns>destination is returned</returns>
        public static Pointer<byte> strncpy(Pointer<byte> dest, Pointer<byte> source, uint n)
        {
            uint c;
            for (c = 0; source[c] != 0 && c < n; c++)
            {
                dest[c] = source[c];
            }

            // Pad the rest with null
            for (; c < n; c++)
            {
                dest[c] = 0;
            }

            return dest;
        }

        /// <summary>
        /// Appends the first num characters of source to destination, plus a terminating null-character.
        /// If the length of the C string in source is less than num, only the content up to the terminating null-character is copied.
        /// </summary>
        /// <param name="dest">Pointer to the destination array, which should contain a C string, and be large enough to contain the concatenated resulting string, including the additional null-character.</param>
        /// <param name="source">C string to be appended.</param>
        /// <param name="num">Maximum number of characters to be appended.</param>
        /// <returns>destination is returned.</returns>
        public static Pointer<byte> strncat(Pointer<byte> dest, Pointer<byte> source, uint num)
        {
            // Seek to the end of dest
            Pointer<byte> d = dest;
            while (d.Deref != 0)
            {
                d = d.Point(1);
            }

            // Start copying
            uint chars_copied = 0;
            while (source.Deref != 0 && chars_copied < num)
            {
                d[0] = source[0];
                chars_copied++;
                source = source.Point(1);
                d = d.Point(1);
            }

            // Null terminate the destination
            d[0] = 0;

            return dest;
        }

        /// <summary>
        /// Returns a pointer to the first occurrence of str2 in str1, or a null pointer if str2 is not part of str1.
        /// The matching process does not include the terminating null-characters, but it stops there.
        /// </summary>
        /// <param name="str1">The string to look within</param>
        /// <param name="str2">The string to search for</param>
        /// <returns>A pointer to the first occurrence in str1 of the entire sequence of characters specified in str2, or a null pointer if the sequence is not present in str1.</returns>
        public static Pointer<byte> strstr(Pointer<byte> str1, Pointer<byte> str2)
        {
            uint len1 = strlen(str1);
            uint len2 = strlen(str2);

            if (len1 < len2)
            {
                return PointerHelpers.NULL<byte>();
            }

            // Iterate through all potential starting points of the substring
            for (int c = 0; c <= len1 - len2; c++)
            {
                int matchLen = 0;
                for (matchLen = 0; matchLen < len2; matchLen++)
                {
                    if (str1[c + matchLen] != str2[matchLen])
                    {
                        break;
                    }
                }

                if (matchLen == len2)
                {
                    return str1.Point(c);
                }
            }

            return PointerHelpers.NULL<byte>();
        }

        public static int atoi(Pointer<byte> str)
        {
            int cursor = 0;
            while ((str[cursor] >= '0' && str[cursor] <= '9') ||
                str[cursor] == '-' ||
                str[cursor] == '+')
            {
                cursor++;
            }

            string strs = FromCString(str, (uint)cursor);
            return int.Parse(strs);
        }

        /// <summary>
        /// Converts a Unicode string into a UTF8 null-terminated c-String
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Pointer<byte> ToCString(string input)
        {
            byte[] encoded = Encoding.UTF8.GetBytes(input);
            byte[] nullTerminated = new byte[encoded.Length + 1];
            nullTerminated[encoded.Length] = 0;
            Array.Copy(encoded, 0, nullTerminated, 0, encoded.Length);
            return nullTerminated.GetPointer();
        }

        /// <summary>
        /// Converts a UTF8 C-string to a unicode string
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string FromCString(Pointer<byte> input)
        {
            uint length = strlen(input);
            byte[] encoded = new byte[length];
            input.MemCopyTo(encoded, 0, encoded.Length);
            return Encoding.UTF8.GetString(encoded, 0, encoded.Length);
        }

        public static string FromCString(Pointer<byte> input, uint length)
        {
            byte[] encoded = new byte[length];
            input.MemCopyTo(encoded, 0, encoded.Length);
            return Encoding.UTF8.GetString(encoded, 0, encoded.Length);
        }
    }
}
