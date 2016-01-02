﻿using System;
using System.Collections.Generic;

namespace Medo.Security.Cryptography.PasswordSafe {
    /// <summary>
    /// Entry.
    /// </summary>
    public class Entry {

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public Entry()
            : this(new Record[] {
                new Record(RecordType.Uuid, Guid.NewGuid().ToByteArray()),
                new Record(RecordType.Title, new byte[0]),
                new Record(RecordType.Password, new byte[0])
            }) {
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="title">Title.</param>
        public Entry(string title) : this() {
            this.Title = title;
        }


        internal Entry(ICollection<Record> records) {
            this.Records = new RecordCollection(this, records);
        }


        internal EntryCollection Owner { get; set; }

        /// <summary>
        /// Used to mark document as changed.
        /// </summary>
        protected void MarkAsChanged() {
            if (this.Owner != null) { this.Owner.MarkAsChanged(); }
        }


        /// <summary>
        /// Gets/sets UUID.
        /// </summary>
        public Guid Uuid {
            get { return this.Records.Contains(RecordType.Uuid) ? this.Records[RecordType.Uuid].Uuid : Guid.Empty; }
            set { this.Records[RecordType.Uuid].Uuid = value; }
        }

        /// <summary>
        /// Gets/sets group.
        /// </summary>
        public string Group {
            get { return this.Records.Contains(RecordType.Group) ? this.Records[RecordType.Group].Text : ""; }
            set { this.Records[RecordType.Group].Text = value; }
        }


        /// <summary>
        /// Gets/sets title.
        /// </summary>
        public string Title {
            get { return this.Records.Contains(RecordType.Title) ? this.Records[RecordType.Title].Text : ""; }
            set { this.Records[RecordType.Title].Text = value; }
        }

        /// <summary>
        /// Gets/sets user name.
        /// </summary>
        public string UserName {
            get { return this.Records.Contains(RecordType.UserName) ? this.Records[RecordType.UserName].Text : ""; }
            set { this.Records[RecordType.UserName].Text = value; }
        }

        /// <summary>
        /// Gets/sets notes.
        /// </summary>
        public string Notes {
            get { return this.Records.Contains(RecordType.Notes) ? this.Records[RecordType.Notes].Text : ""; }
            set { this.Records[RecordType.Notes].Text = value; }
        }

        /// <summary>
        /// Gets/sets password.
        /// </summary>
        public string Password {
            get { return this.Records.Contains(RecordType.Password) ? this.Records[RecordType.Password].Text : ""; }
            set { this.Records[RecordType.Password].Text = value; }
        }


        /// <summary>
        /// Gets/sets creation time.
        /// </summary>
        public DateTime CreationTime {
            get { return this.Records.Contains(RecordType.CreationTime) ? this.Records[RecordType.CreationTime].Time : DateTime.MinValue; }
            set { this.Records[RecordType.CreationTime].Time = value; }
        }

        /// <summary>
        /// Gets/sets password modification time.
        /// </summary>
        public DateTime PasswordModificationTime {
            get { return this.Records.Contains(RecordType.PasswordModificationTime) ? this.Records[RecordType.PasswordModificationTime].Time : DateTime.MinValue; }
            set { this.Records[RecordType.PasswordModificationTime].Time = value; }
        }

        /// <summary>
        /// Gets/sets last access time.
        /// </summary>
        public DateTime LastAccessTime {
            get { return this.Records.Contains(RecordType.LastAccessTime) ? this.Records[RecordType.LastAccessTime].Time : DateTime.MinValue; }
            set { this.Records[RecordType.LastAccessTime].Time = value; }
        }

        /// <summary>
        /// Gets/sets password expiry time.
        /// </summary>
        public DateTime PasswordExpiryTime {
            get { return this.Records.Contains(RecordType.PasswordExpiryTime) ? this.Records[RecordType.PasswordExpiryTime].Time : DateTime.MinValue; }
            set { this.Records[RecordType.PasswordExpiryTime].Time = value; }
        }

        /// <summary>
        /// Gets/sets last modification time.
        /// </summary>
        public DateTime LastModificationTime {
            get { return this.Records.Contains(RecordType.LastModificationTime) ? this.Records[RecordType.LastModificationTime].Time : DateTime.MinValue; }
            set { this.Records[RecordType.LastModificationTime].Time = value; }
        }


        /// <summary>
        /// Gets/sets URL.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Password Safe file format doesn't require this URL to follow URL format.")]
        public string Url {
            get { return this.Records.Contains(RecordType.Url) ? this.Records[RecordType.Url].Text : ""; }
            set { this.Records[RecordType.Url].Text = value; }
        }


        /// <summary>
        /// Gets list of records.
        /// </summary>
        public RecordCollection Records { get; }


        /// <summary>
        /// Returns a string representation of an object.
        /// </summary>
        public override string ToString() {
            return this.Records.Contains(RecordType.Title) ? this.Records[RecordType.Title].Text : "";
        }


        #region ICollection extra

        /// <summary>
        /// Gets field based on a type.
        /// If multiple elements exist with the same field type, the first one is returned.
        /// If type does not exist, it is created.
        /// 
        /// If value is set to null, field is removed.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <exception cref="ArgumentOutOfRangeException">Only null value is supported.</exception>
        public Record this[RecordType type] {
            get { return this.Records[type]; }
            set { this.Records[type] = value; }
        }

        #endregion

    }
}
