﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace InitVent.Common.Security
{
    /// <summary>
    /// Salted password hashing with PBKDF2-SHA1.
    /// </summary>
    /// <remarks>
    /// Adapted from original work written by: havoc AT defuse.ca
    /// www: http://crackstation.net/hashing-security.htm
    /// </remarks>
    public class PasswordHash
    {
        /// <summary>
        /// The character separator to use when serializing the password hash.
        /// </summary>
        public const char Separator = ':';

        /// <summary>
        /// The number of iterations used within the PBKDF2 process.
        /// </summary>
        public int Pbkdf2Iterations { get; private set; }

        /// <summary>
        /// The randomly generated salt.
        /// </summary>
        public byte[] Salt { get; private set; }

        /// <summary>
        /// The generated hash.
        /// </summary>
        public byte[] Hash { get; private set; }

        /// <summary>
        /// Creates a new password hash from the specified salt, hash, and PBKDF2 iterations.
        /// </summary>
        /// <param name="salt">The randomly generated salt.</param>
        /// <param name="hash">The generated hash.</param>
        /// <param name="iterations">The number of iterations used within the PBKDF2 process.</param>
        /// <remarks>
        /// This constructor is only necessary if implementors decline to store the composite hash
        /// using the standard ToString() method.
        /// </remarks>
        /// <see cref="PasswordHash.ToString()"/>
        /// <see cref="PasswordHash.Parse()"/>
        public PasswordHash(byte[] salt, byte[] hash, int iterations)
        {
            Pbkdf2Iterations = iterations;
            Salt = salt;
            Hash = hash;
        }

        /// <summary>
        /// Validates a password against this hash.
        /// </summary>
        /// <param name="password">The password to check.</param>
        /// <returns>True if the password is correct; false otherwise.</returns>
        public bool CheckPassword(String password)
        {
            byte[] testHash = ComputeHash(password, Salt, Pbkdf2Iterations, Hash.Length);
            return SlowEquals(Hash, testHash);
        }

        /// <summary>
        /// Serializes this hash to a base-64 string.  This can later be recovered through the Parse()
        /// method.
        /// </summary>
        /// <returns>A base-64 representation of the entire hash.</returns>
        /// <seealso cref="PasswordHash.Parse()"/>
        public override String ToString()
        {
            return String.Format("{1:X}{0}{2}{0}{3}", Separator, Pbkdf2Iterations, Convert.ToBase64String(Salt), Convert.ToBase64String(Hash));
        }

        /// <summary>
        /// Parses a password hash from the given string, assuming it was generated by the
        /// ToString() method.
        /// </summary>
        /// <param name="compositeHash">A hash of a correct password.</param>
        /// <returns>An object representation of the hash.</returns>
        /// <exception cref="System.FormatException">If the given string is not in a recognized format.</exception>
        /// <see cref="PasswordHash.ToString()"/>
        public static PasswordHash Parse(String compositeHash)
        {
            String[] split = compositeHash.Split(':');

            int iterations = Int32.Parse(split[0], System.Globalization.NumberStyles.HexNumber);
            byte[] salt = Convert.FromBase64String(split[1]);
            byte[] hash = Convert.FromBase64String(split[2]);

            return new PasswordHash(salt, hash, iterations);
        }

        /// <summary>
        /// Creates a new hash for the given password.  This also generates a new random salt.
        /// </summary>
        /// <param name="password">The password to hash.</param>
        /// <param name="hashBytes">The number of bytes to output; also the size of salt.</param>
        /// <param name="iterations">The number of iterations to use within the PBKDF2 process.</param>
        /// <returns>A new password hash with a fresh random salt.</returns>
        /// <remarks>
        /// The optional parameters may be changed without breaking existing hashes.
        /// </remarks>
        public static PasswordHash Create(String password, int hashBytes = 24, int iterations = 10000)
        {
            var salt = GenerateRandomSalt(hashBytes);
            var hash = ComputeHash(password, salt, iterations, hashBytes);

            return new PasswordHash(salt, hash, iterations);
        }

        /// <summary>
        /// Compares two byte arrays in length-constant time. This comparison
        /// method is used so that password hashes cannot be extracted from
        /// on-line systems using a timing attack and then attacked off-line.
        /// </summary>
        /// <param name="a">The first byte array.</param>
        /// <param name="b">The second byte array.</param>
        /// <returns>True if both byte arrays are equal; false otherwise.</returns>
        protected static bool SlowEquals(byte[] a, byte[] b)
        {
            uint diff = (uint)a.Length ^ (uint)b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
                diff |= (uint)(a[i] ^ b[i]);

            return diff == 0;
        }

        /// <summary>
        /// Generates a random cryptographic salt with the specified number of bytes.
        /// </summary>
        /// <param name="saltBytes">The number of bytes to use in the salt.</param>
        /// <returns>A random salt.</returns>
        protected static byte[] GenerateRandomSalt(int saltBytes)
        {
            var salt = new byte[saltBytes];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        /// <summary>
        /// Computes the PBKDF2-SHA1 hash of a password.
        /// </summary>
        /// <param name="password">The password to hash.</param>
        /// <param name="salt">The salt.</param>
        /// <param name="iterations">The PBKDF2 iteration count.</param>
        /// <param name="outputBytes">The length of the hash to generate, in bytes.</param>
        /// <returns>A hash of the password.</returns>
        protected static byte[] ComputeHash(String password, byte[] salt, int iterations, int outputBytes)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations))
            {
                return pbkdf2.GetBytes(outputBytes);
            }
        }
    }
}
