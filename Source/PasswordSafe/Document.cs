﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Medo.Security.Cryptography.PasswordSafe {
    /// <summary>
    /// Password Safe document.
    /// </summary>
    public class Document : IDisposable {

        private Document() {
            this.Headers = new HeaderCollection(this, new Header[] {
                new Header(HeaderType.Version, BitConverter.GetBytes(Header.DefaultVersion)),
                new Header(HeaderType.Uuid,Guid.NewGuid().ToByteArray()),
            });
            this.Entries = new EntryCollection(this);

            this.TrackAccess = true;
            this.TrackModify = true;
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="passphraseBuffer">Password bytes. Caller has to avoid keeping bytes unencrypted in memory.</param>
        public Document(byte[] passphraseBuffer)
            : this() {
            if (passphraseBuffer == null) { throw new ArgumentNullException(nameof(passphraseBuffer), "Passphrase cannot be null."); }

            this.Passphrase = passphraseBuffer; //no need for copy - will be done in property setter
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="passphrase">Password.</param>
        public Document(string passphrase)
            : this() {
            if (passphrase == null) { throw new ArgumentNullException(nameof(passphrase), "Passphrase cannot be null."); }

            var passphraseBuffer = Utf8Encoding.GetBytes(passphrase);
            try {
                this.Passphrase = passphraseBuffer; //no need for copy - will be done in property setter
            } finally {
                Array.Clear(passphraseBuffer, 0, passphraseBuffer.Length); //remove passphrase bytes from memory - nothing to do about the string. :(
            }
        }


        internal Document(ICollection<Header> headers, params ICollection<Record>[] records) {
            this.Headers = new HeaderCollection(this, headers);
            this.Entries = new EntryCollection(this, records);

            this.TrackAccess = true;
            this.TrackModify = true;
        }


        /// <summary>
        /// Gets/sets database version.
        /// </summary>
        public int Version {
            get { return this.Headers[HeaderType.Version].Version; }
            set { this.Headers[HeaderType.Version].Version = value; }
        }


        /// <summary>
        /// Gets/sets UUID.
        /// </summary>
        public Guid Uuid {
            get { return this.Headers.Contains(HeaderType.Uuid) ? this.Headers[HeaderType.Uuid].Uuid : Guid.Empty; }
            set { this.Headers[HeaderType.Uuid].Uuid = value; }
        }

        /// <summary>
        /// Gets/sets last save time.
        /// </summary>
        public DateTime LastSaveTime {
            get { return this.Headers.Contains(HeaderType.TimestampOfLastSave) ? this.Headers[HeaderType.TimestampOfLastSave].Time : DateTime.MinValue; }
            set { this.Headers[HeaderType.TimestampOfLastSave].Time = value; }
        }

        /// <summary>
        /// Gets/sets last save application.
        /// </summary>
        public string LastSaveApplication {
            get { return this.Headers.Contains(HeaderType.WhatPerformedLastSave) ? this.Headers[HeaderType.WhatPerformedLastSave].Text : ""; }
            set { this.Headers[HeaderType.WhatPerformedLastSave].Text = value; }
        }

        /// <summary>
        /// Gets/sets last save user.
        /// </summary>
        public string LastSaveUser {
            get { return this.Headers.Contains(HeaderType.LastSavedByUser) ? this.Headers[HeaderType.LastSavedByUser].Text : ""; }
            set { this.Headers[HeaderType.LastSavedByUser].Text = value; }
        }

        /// <summary>
        /// Gets/sets last save computer.
        /// </summary>
        public string LastSaveHost {
            get { return this.Headers.Contains(HeaderType.LastSavedOnHost) ? this.Headers[HeaderType.LastSavedOnHost].Text : ""; }
            set { this.Headers[HeaderType.LastSavedOnHost].Text = value; }
        }

        /// <summary>
        /// Gets/sets database name.
        /// </summary>
        public string Name {
            get { return this.Headers.Contains(HeaderType.DatabaseName) ? this.Headers[HeaderType.DatabaseName].Text : ""; }
            set { this.Headers[HeaderType.DatabaseName].Text = value; }
        }

        /// <summary>
        /// Gets/sets database description.
        /// </summary>
        public string Description {
            get { return this.Headers.Contains(HeaderType.DatabaseDescription) ? this.Headers[HeaderType.DatabaseDescription].Text : ""; }
            set { this.Headers[HeaderType.DatabaseDescription].Text = value; }
        }


        private int _iterations = 2048;
        /// <summary>
        /// Gets/sets desired number of iterations.
        /// Cannot be less than 2048.
        /// </summary>
        public int Iterations {
            get { return this._iterations; }
            set {
                if (value < 2048) { value = 2048; }
                this._iterations = value;
            }
        }


        /// <summary>
        /// Gets list of headers.
        /// </summary>
        /// 
        public HeaderCollection Headers { get; }

        /// <summary>
        /// Gets list of entries.
        /// </summary>
        public EntryCollection Entries { get; }


        #region Load/Save

        private const int Tag = 0x33535750; //PWS3 in LE
        private const int TagEof = 0x464F452D; //-EOF in LE
        private static readonly Encoding Utf8Encoding = new UTF8Encoding(false);

        /// <summary>
        /// Loads data from a file.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <param name="passphrase">Password.</param>
        /// <exception cref="ArgumentNullException">Stream cannot be null. -or- Passphrase cannot be null.</exception>
        /// <exception cref="FormatException">Unrecognized file format.</exception>
        /// <exception cref="CryptographicException">Password mismatch. -or- Authentication mismatch.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "It is up to a caller to Dispose newly created document.")]
        public static Document Load(Stream stream, string passphrase) {
            if (stream == null) { throw new ArgumentNullException(nameof(stream), "Stream cannot be null."); }
            if (passphrase == null) { throw new ArgumentNullException(nameof(passphrase), "Passphrase cannot be null."); }

            var passphraseBytes = Utf8Encoding.GetBytes(passphrase);
            try {
                return Load(stream, passphraseBytes);
            } finally {
                Array.Clear(passphraseBytes, 0, passphraseBytes.Length); //remove passphrase bytes from memory - nothing to do about the string. :(
            }
        }

        /// <summary>
        /// Loads data from a file.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <param name="passphraseBuffer">Password bytes. Caller has to avoid keeping bytes unencrypted in memory.</param>
        /// <exception cref="ArgumentNullException">Stream cannot be null. -or- Passphrase cannot be null.</exception>
        /// <exception cref="FormatException">Unrecognized file format.</exception>
        /// <exception cref="CryptographicException">Password mismatch. -or- Authentication mismatch.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "It is up to a caller to Dispose newly created document.")]
        public static Document Load(Stream stream, byte[] passphraseBuffer) {
            if (stream == null) { throw new ArgumentNullException(nameof(stream), "Stream cannot be null."); }
            if (passphraseBuffer == null) { throw new ArgumentNullException(nameof(passphraseBuffer), "Passphrase cannot be null."); }

            byte[] buffer = new byte[16384];
            using (var ms = new MemoryStream()) {
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0) {
                    ms.Write(buffer, 0, read);
                }
                buffer = ms.ToArray();
            }

            if ((buffer.Length < 200)
              || (BitConverter.ToInt32(buffer, 0) != Tag)
              || (BitConverter.ToInt32(buffer, buffer.Length - 32 - 16) != Tag)
              || (BitConverter.ToInt32(buffer, buffer.Length - 32 - 12) != TagEof)
              || (BitConverter.ToInt32(buffer, buffer.Length - 32 - 8) != Tag)
              || (BitConverter.ToInt32(buffer, buffer.Length - 32 - 4) != TagEof)) {
                throw new FormatException("Unrecognized file format.");
            }

            var salt = new byte[32];
            Buffer.BlockCopy(buffer, 4, salt, 0, salt.Length);

            var iter = BitConverter.ToUInt32(buffer, 36);

            byte[] stretchedKey = null, keyK = null, keyL = null, data = null;
            try {
                stretchedKey = GetStretchedKey(passphraseBuffer, salt, iter);
                if (!AreBytesTheSame(GetSha256Hash(stretchedKey), buffer, 40)) {
                    throw new CryptographicException("Password mismatch.");
                }

                keyK = DecryptKey(stretchedKey, buffer, 72);
                keyL = DecryptKey(stretchedKey, buffer, 104);

                var iv = new byte[16];
                Buffer.BlockCopy(buffer, 136, iv, 0, iv.Length);

                data = DecryptData(keyK, iv, buffer, 152, buffer.Length - 200);

                using (var dataHash = new HMACSHA256(keyL)) {
                    int dataOffset = 0;

                    var headerFields = new List<Header>();
                    while (dataOffset < data.Length) {
                        var fieldLength = BitConverter.ToInt32(data, dataOffset + 0);
                        var fieldLengthFull = ((fieldLength + 5) / 16 + 1) * 16;
                        var fieldType = (HeaderType)data[dataOffset + 4];
                        var fieldData = new byte[fieldLength];
                        try {
                            Buffer.BlockCopy(data, dataOffset + 5, fieldData, 0, fieldLength);
                            dataOffset += fieldLengthFull; //there is ALWAYS some random bytes added, thus extra block if 16 bytes

                            dataHash.TransformBlock(fieldData, 0, fieldData.Length, null, 0); //not hashing length nor type - wtf?
                            if (fieldType == HeaderType.EndOfEntry) { break; }

                            headerFields.Add(new Header(fieldType, fieldData));
                        } finally {
                            Array.Clear(fieldData, 0, fieldData.Length);
                        }
                    }

                    if ((headerFields.Count == 0) || (headerFields[0].Version < 0x0300)) { throw new FormatException("Unrecognized file format version."); }

                    var recordFields = new List<List<Record>>();
                    List<Record> records = null;
                    while (dataOffset < data.Length) {
                        var fieldLength = BitConverter.ToInt32(data, dataOffset + 0);
                        var fieldLengthFull = (fieldLength / 16 + 1) * 16;
                        var fieldType = (RecordType)data[dataOffset + 4];
                        var fieldData = new byte[fieldLength];
                        try {
                            Buffer.BlockCopy(data, dataOffset + 5, fieldData, 0, fieldLength);
                            dataOffset += fieldLengthFull; //there is ALWAYS some random bytes added, thus extra block if 16 bytes

                            dataHash.TransformBlock(fieldData, 0, fieldData.Length, null, 0); //not hashing length nor type - wtf?
                            if (fieldType == RecordType.EndOfEntry) { records = null; continue; }

                            if (records == null) {
                                records = new List<Record>();
                                recordFields.Add(records);
                            }
                            records.Add(new Record(fieldType, fieldData));
                        } finally {
                            Array.Clear(fieldData, 0, fieldData.Length);
                        }
                    }

                    dataHash.TransformFinalBlock(new byte[] { }, 0, 0);

                    if (!AreBytesTheSame(dataHash.Hash, buffer, buffer.Length - 32)) {
                        throw new CryptographicException("Authentication mismatch.");
                    }

                    var document = new Document(headerFields, recordFields.ToArray());
                    document._iterations = (int)iter; //to avoid rounding up if iteration count is less than 2048
                    document.Passphrase = passphraseBuffer; //to avoid keeping password in memory for save - at least we don't need to deal with string's immutability.
                    return document;
                }
            } finally { //best effort to sanitize memory
                if (stretchedKey != null) { Array.Clear(stretchedKey, 0, stretchedKey.Length); }
                if (keyK != null) { Array.Clear(keyK, 0, keyK.Length); }
                if (keyL != null) { Array.Clear(keyL, 0, keyL.Length); }
                if (data != null) { Array.Clear(data, 0, data.Length); }
            }
        }


        /// <summary>
        /// Save document using the same password as for Load.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <exception cref="ArgumentNullException">Stream cannot be null.</exception>
        /// <exception cref="NotSupportedException">Missing passphrase.</exception>
        public void Save(Stream stream) {
            if (stream == null) { throw new ArgumentNullException(nameof(stream), "Stream cannot be null."); }

            var passphraseBytes = this.Passphrase;
            if (passphraseBytes == null) { throw new NotSupportedException("Missing passphrase."); }
            try {
                Save(stream, passphraseBytes);
            } finally {
                Array.Clear(passphraseBytes, 0, passphraseBytes.Length); //remove passphrase bytes from memory - nothing to do about the string. :(
            }
        }

        /// <summary>
        /// Save document.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <param name="passphrase">Password.</param>
        /// <exception cref="ArgumentNullException">Stream cannot be null. -or- Passphrase cannot be null.</exception>
        public void Save(Stream stream, string passphrase) {
            if (stream == null) { throw new ArgumentNullException(nameof(stream), "Stream cannot be null."); }
            if (passphrase == null) { throw new ArgumentNullException(nameof(passphrase), "Passphrase cannot be null."); }

            var passphraseBytes = Utf8Encoding.GetBytes(passphrase);
            try {
                Save(stream, passphraseBytes);
            } finally {
                Array.Clear(passphraseBytes, 0, passphraseBytes.Length); //remove passphrase bytes from memory - nothing to do about the string. :(
            }
        }


        /// <summary>
        /// Save document.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <param name="passphraseBuffer">Password bytes. Caller has to avoid keeping bytes unencrypted in memory.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "Medo.Security.Cryptography.PasswordSafe.Field.set_Text(System.String)", Justification = "String is not exposed to the end user.")]
        public void Save(Stream stream, byte[] passphraseBuffer) {
            if (stream == null) { throw new ArgumentNullException(nameof(stream), "Stream cannot be null."); }
            if (passphraseBuffer == null) { throw new ArgumentNullException(nameof(passphraseBuffer), "Passphrase cannot be null."); }

            if (!this.IsReadOnly && this.TrackModify) {
                this.Headers[HeaderType.TimestampOfLastSave].Time = DateTime.UtcNow;

                var assemblyName = Assembly.GetExecutingAssembly().GetName();
                this.Headers[HeaderType.WhatPerformedLastSave].Text = string.Format(CultureInfo.InvariantCulture, "{0} V{1}.{2:00}", assemblyName.Name, assemblyName.Version.Major, assemblyName.Version.Minor);

                this.Headers[HeaderType.LastSavedByUser].Text = Environment.UserName;
                this.Headers[HeaderType.LastSavedOnHost].Text = Environment.MachineName;
            }

            byte[] stretchedKey = null;
            byte[] keyK = null;
            byte[] keyL = null;
            //byte[] data = null;
            try {
                stream.Write(BitConverter.GetBytes(Tag), 0, 4);

                var salt = new byte[32];
                Rnd.GetBytes(salt);
                stream.Write(salt, 0, salt.Length);

                this.Iterations = this.Iterations; //to force minimum iteration count
                var iter = (uint)this.Iterations;
                stream.Write(BitConverter.GetBytes(iter), 0, 4);

                stretchedKey = GetStretchedKey(passphraseBuffer, salt, iter);
                stream.Write(GetSha256Hash(stretchedKey), 0, 32);

                keyK = new byte[32];
                Rnd.GetBytes(keyK);
                stream.Write(EncryptKey(stretchedKey, keyK, 0), 0, 32);

                keyL = new byte[32];
                Rnd.GetBytes(keyL);
                stream.Write(EncryptKey(stretchedKey, keyL, 0), 0, 32);

                var iv = new byte[16];
                Rnd.GetBytes(iv);
                stream.Write(iv, 0, iv.Length);

                using (var dataHash = new HMACSHA256(keyL))
                using (var twofish = new TwofishManaged()) {
                    twofish.Mode = CipherMode.CBC;
                    twofish.Padding = PaddingMode.None;
                    twofish.KeySize = 256;
                    twofish.Key = keyK;
                    twofish.IV = iv;
                    using (var dataEncryptor = twofish.CreateEncryptor()) {
                        foreach (var field in this.Headers) {
                            WriteBlock(stream, dataHash, dataEncryptor, (byte)field.HeaderType, field.RawData);
                        }
                        WriteBlock(stream, dataHash, dataEncryptor, (byte)HeaderType.EndOfEntry, new byte[] { });

                        foreach (var entry in this.Entries) {
                            foreach (var field in entry.Records) {
                                WriteBlock(stream, dataHash, dataEncryptor, (byte)field.RecordType, field.RawData);
                            }
                            WriteBlock(stream, dataHash, dataEncryptor, (byte)RecordType.EndOfEntry, new byte[] { });
                        }
                    }

                    dataHash.TransformFinalBlock(new byte[] { }, 0, 0);

                    stream.Write(BitConverter.GetBytes(Tag), 0, 4);
                    stream.Write(BitConverter.GetBytes(TagEof), 0, 4);
                    stream.Write(BitConverter.GetBytes(Tag), 0, 4);
                    stream.Write(BitConverter.GetBytes(TagEof), 0, 4);

                    stream.Write(dataHash.Hash, 0, dataHash.Hash.Length);
                    this.HasChanged = false;
                }
            } finally {
                if (stretchedKey != null) { Array.Clear(stretchedKey, 0, stretchedKey.Length); }
                if (keyK != null) { Array.Clear(keyK, 0, keyK.Length); }
                if (keyL != null) { Array.Clear(keyL, 0, keyL.Length); }
                //if (data != null) { Array.Clear(data, 0, data.Length); }
            }
        }

        private static void WriteBlock(Stream stream, HashAlgorithm dataHash, ICryptoTransform dataEncryptor, byte type, byte[] fieldData) {
            dataHash.TransformBlock(fieldData, 0, fieldData.Length, null, 0);

            byte[] fieldBlock = null;
            try {
                var fieldLengthPadded = ((fieldData.Length + 5) / 16 + 1) * 16;
                fieldBlock = new byte[fieldLengthPadded];

                Rnd.GetBytes(fieldBlock);
                Buffer.BlockCopy(BitConverter.GetBytes(fieldData.Length), 0, fieldBlock, 0, 4);
                fieldBlock[4] = type;
                Buffer.BlockCopy(fieldData, 0, fieldBlock, 5, fieldData.Length);

                dataEncryptor.TransformBlock(fieldBlock, 0, fieldBlock.Length, fieldBlock, 0);
                stream.Write(fieldBlock, 0, fieldBlock.Length);
            } finally {
                Array.Clear(fieldData, 0, fieldData.Length);
                if (fieldBlock != null) { Array.Clear(fieldBlock, 0, fieldBlock.Length); }
            }
        }

        private static RandomNumberGenerator Rnd = RandomNumberGenerator.Create();
        private byte[] PassphraseEntropy = new byte[16];

        private byte[] _passphrase;
        /// <summary>
        /// Gets/sets passphrase.
        /// Bytes are kept encrypted in memory until accessed.
        /// </summary>
        private byte[] Passphrase {
            get {
                return (this._passphrase != null) ? ProtectedData.Unprotect(this._passphrase, this.PassphraseEntropy, DataProtectionScope.CurrentUser) : null;
            }
            set {
                Rnd.GetBytes(PassphraseEntropy);
                this._passphrase = ProtectedData.Protect(value, this.PassphraseEntropy, DataProtectionScope.CurrentUser);
                Array.Clear(value, 0, value.Length);
            }
        }

        #endregion


        /// <summary>
        /// Gets/sets if document is read-only.
        /// </summary>
        public bool IsReadOnly { get; set; }


        /// <summary>
        /// Gets if document will automatically fill fields with access information.
        /// </summary>
        public bool TrackAccess { get; set; }

        /// <summary>
        /// Gets if document will automatically fill fields with modification information.
        /// </summary>
        public bool TrackModify { get; set; }


        private bool _hasChanged;
        /// <summary>
        /// Gets is document has been changed since last save.
        /// </summary>
        public bool HasChanged {
            get { return this._hasChanged; }
            private set { this._hasChanged = value; }
        }

        internal void MarkAsChanged() {
            this.HasChanged = true;
        }


        #region Utility functions

        private static byte[] DecryptKey(byte[] stretchedKey, byte[] buffer, int offset) {
            using (var twofish = new TwofishManaged()) {
                twofish.Mode = CipherMode.ECB;
                twofish.Padding = PaddingMode.None;
                twofish.KeySize = 256;
                twofish.Key = stretchedKey;
                using (var transform = twofish.CreateDecryptor()) {
                    return transform.TransformFinalBlock(buffer, offset, 32);
                }
            }
        }

        private static byte[] EncryptKey(byte[] stretchedKey, byte[] buffer, int offset) {
            using (var twofish = new TwofishManaged()) {
                twofish.Mode = CipherMode.ECB;
                twofish.Padding = PaddingMode.None;
                twofish.KeySize = 256;
                twofish.Key = stretchedKey;
                using (var transform = twofish.CreateEncryptor()) {
                    return transform.TransformFinalBlock(buffer, offset, 32);
                }
            }
        }

        private static byte[] DecryptData(byte[] key, byte[] iv, byte[] buffer, int offset, int length) {
            using (var twofish = new TwofishManaged()) {
                twofish.Mode = CipherMode.CBC;
                twofish.Padding = PaddingMode.None;
                twofish.KeySize = 256;
                twofish.Key = key;
                twofish.IV = iv;
                using (var dataDecryptor = twofish.CreateDecryptor()) {
                    return dataDecryptor.TransformFinalBlock(buffer, offset, length);
                }
            }
        }

        private static byte[] GetStretchedKey(byte[] passphrase, byte[] salt, uint iterations) {
            var hash = GetSha256Hash(passphrase, salt);
            for (var i = 0; i < iterations; i++) {
                hash = GetSha256Hash(hash);
            }
            return hash;
        }

        private static byte[] GetSha256Hash(params byte[][] buffers) {
            using (var hash = new SHA256Managed()) {
                foreach (var buffer in buffers) {
                    hash.TransformBlock(buffer, 0, buffer.Length, buffer, 0);
                }
                hash.TransformFinalBlock(new byte[] { }, 0, 0);
                return hash.Hash;
            }
        }

        private static bool AreBytesTheSame(byte[] buffer1, byte[] buffer2, int buffer2Offset) {
            if (buffer1.Length == 0) { return false; }
            if (buffer2Offset + buffer1.Length > buffer2.Length) { return false; }
            for (int i = 0; i < buffer1.Length; i++) {
                if (buffer1[i] != buffer2[buffer2Offset + i]) { return false; }
            }
            return true;
        }

        #endregion


        #region IDisposable

        /// <summary>
        /// Disposes resources.
        /// </summary>
        /// <param name="disposing">True if managed resources are to be disposed.</param>
        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                if (this._passphrase != null) { Array.Clear(this._passphrase, 0, this._passphrase.Length); }
            }
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
