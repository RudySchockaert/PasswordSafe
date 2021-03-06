using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Medo.Security.Cryptography.PasswordSafe {
    /// <summary>
    /// Collection of entries.
    /// </summary>
    [DebuggerDisplay("{Count} entries")]
    public class EntryCollection : IList<Entry> {

        /// <summary>
        /// Create a new instance.
        /// </summary>
        internal EntryCollection(Document owner, params ICollection<Record>[] records) {
            this.Owner = owner;
            foreach (var entry in records) {
                this.BaseCollection.Add(new Entry(entry) { Owner = this });
            }
        }


        internal Document Owner { get; set; }

        internal void MarkAsChanged() {
            this.Owner.MarkAsChanged();
        }


        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private readonly List<Entry> BaseCollection = new List<Entry>();


        #region ICollection

        /// <summary>
        /// Adds an item.
        /// </summary>
        /// <param name="item">Item.</param>
        /// <exception cref="ArgumentNullException">Item cannot be null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Item cannot be in other collection.</exception>
        /// <exception cref="NotSupportedException">Collection is read-only.</exception>
        public void Add(Entry item) {
            if (item == null) { throw new ArgumentNullException(nameof(item), "Item cannot be null."); }
            if (item.Owner != null) { throw new ArgumentOutOfRangeException(nameof(item), "Item cannot be in other collection."); }
            if (this.IsReadOnly) { throw new NotSupportedException("Collection is read-only."); }

            this.BaseCollection.Add(item);
            item.Owner = this;
            this.MarkAsChanged();
        }

        /// <summary>
        /// Adds multiple items.
        /// </summary>
        /// <param name="items">Item.</param>
        /// <exception cref="ArgumentNullException">Items cannot be null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Item cannot be in other collection.</exception>
        /// <exception cref="NotSupportedException">Collection is read-only.</exception>
        public void AddRange(IEnumerable<Entry> items) {
            if (items == null) { throw new ArgumentNullException(nameof(items), "Item cannot be null."); }
            foreach (var item in items) {
                if (item.Owner != null) { throw new ArgumentOutOfRangeException(nameof(items), "Item cannot be in other collection."); }
            }
            if (this.IsReadOnly) { throw new NotSupportedException("Collection is read-only."); }

            this.BaseCollection.AddRange(items);
            foreach (var item in items) {
                item.Owner = this;
            }
            this.MarkAsChanged();
        }

        /// <summary>
        /// Removes all items.
        /// </summary>
        /// <exception cref="NotSupportedException">Collection is read-only.</exception>
        public void Clear() {
            if (this.IsReadOnly) { throw new NotSupportedException("Collection is read-only."); }

            foreach (var item in this.BaseCollection) {
                item.Owner = null;
            }
            this.BaseCollection.Clear();
            this.MarkAsChanged();
        }

        /// <summary>
        /// Determines whether the collection contains a specific item.
        /// </summary>
        /// <param name="item">The item to locate.</param>
        public bool Contains(Entry item) {
            if (item == null) { return false; }
            return this.BaseCollection.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the collection to an array, starting at a particular array index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from collection.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(Entry[] array, int arrayIndex) {
            this.BaseCollection.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Gets the number of items contained in the collection.
        /// </summary>
        public int Count {
            get { return this.BaseCollection.Count; }
        }

        /// <summary>
        /// Searches for the specified item and returns the zero-based index of the first occurrence within the entire collection.
        /// </summary>
        /// <param name="item">The item to locate.</param>
        public int IndexOf(Entry item) {
            return this.BaseCollection.IndexOf(item);
        }

        /// <summary>
        /// Inserts an element into the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="item">The item to insert.</param>
        /// <exception cref="ArgumentNullException">Item cannot be null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Index is less than 0. -or- Index is greater than collection count.</exception>
        /// <exception cref="NotSupportedException">Collection is read-only.</exception>
        public void Insert(int index, Entry item) {
            if (item == null) { throw new ArgumentNullException(nameof(item), "Item cannot be null."); }
            if (item.Owner != null) { throw new ArgumentOutOfRangeException(nameof(item), "Item cannot be in other collection."); }
            if (this.IsReadOnly) { throw new NotSupportedException("Collection is read-only."); }

            this.BaseCollection.Insert(index, item);
            item.Owner = this;
            this.MarkAsChanged();
        }

        /// <summary>
        /// Gets a value indicating whether the collection is read-only.
        /// </summary>
        public bool IsReadOnly {
            get { return this.Owner.IsReadOnly; }
        }

        /// <summary>
        /// Removes the item from the collection.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <exception cref="ArgumentNullException">Item cannot be null.</exception>
        /// <exception cref="NotSupportedException">Collection is read-only.</exception>
        public bool Remove(Entry item) {
            if (item == null) { throw new ArgumentNullException(nameof(item), "Item cannot be null."); }
            if (this.IsReadOnly) { throw new NotSupportedException("Collection is read-only."); }

            if (this.BaseCollection.Remove(item)) {
                item.Owner = null;
                this.MarkAsChanged();
                return true;
            } else {
                return false;
            }
        }

        /// <summary>
        /// Removes the element at the specified index of the collection.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">Index is less than 0. -or- Index is equal to or greater than collection count.</exception>
        /// <exception cref="NotSupportedException">Collection is read-only.</exception>
        public void RemoveAt(int index) {
            if (this.IsReadOnly) { throw new NotSupportedException("Collection is read-only."); }

            var item = this[index];
            this.BaseCollection.Remove(item);
            item.Owner = null;
            this.MarkAsChanged();
        }

        /// <summary>
        /// Exposes the enumerator, which supports a simple iteration over a collection of a specified type.
        /// </summary>
        public IEnumerator<Entry> GetEnumerator() {
            var items = new List<Entry>(this.BaseCollection); //to avoid exception if collection is changed while in foreach
            foreach (var item in items) {
                yield return item;
            }
        }

        /// <summary>
        /// Exposes the enumerator, which supports a simple iteration over a non-generic collection.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <exception cref="ArgumentNullException">Value cannot be null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Index is less than 0. -or- Index is equal to or greater than collection count. -or- Duplicate name in collection.</exception>
        /// <exception cref="NotSupportedException">Collection is read-only.</exception>
        public Entry this[int index] {
            get { return this.BaseCollection[index]; }
            set {
                if (this.IsReadOnly) { throw new NotSupportedException("Collection is read-only."); }
                if (value == null) { throw new ArgumentNullException(nameof(value), "Value cannot be null."); }
                if (value.Owner != null) { throw new ArgumentOutOfRangeException(nameof(value), "Item cannot be in other collection."); }
                if (this.Contains(value)) { throw new ArgumentOutOfRangeException(nameof(value), "Duplicate item in collection."); }

                var item = this.BaseCollection[index];
                item.Owner = null;
                this.BaseCollection.RemoveAt(index);
                this.BaseCollection.Insert(index, value);
                value.Owner = this;
                this.MarkAsChanged();
            }
        }

        #endregion


        #region ICollection extra

        /// <summary>
        /// Determines whether the collection contains an entry with specified title.
        /// Search is case-insensitive.
        /// </summary>
        /// <param name="title">Title.</param>
        public bool Contains(string title) {
            foreach (var entry in this.BaseCollection) {
                if (entry.Title.Equals(title, StringComparison.CurrentCultureIgnoreCase)) { return true; }
            }
            return false;
        }

        /// <summary>
        /// Determines whether the collection contains an entry with specified group and title.
        /// Search is case-insensitive.
        /// </summary>
        /// <param name="group">Group.</param>
        /// <param name="title">Title.</param>
        public bool Contains(GroupPath group, string title) {
            foreach (var entry in this.BaseCollection) {
                if (entry.Group.Equals(group) && entry.Title.Equals(title, StringComparison.CurrentCultureIgnoreCase)) { return true; }
            }
            return false;
        }

        /// <summary>
        /// Gets entry with the specified title.
        /// If multiple elements exist with the same title, the first one is returned.
        /// If type does not exist, it is created.
        /// Search is case-insensitive.
        /// 
        /// If value is set to null, entry is removed.
        /// </summary>
        /// <param name="title">Title.</param>
        /// <exception cref="ArgumentOutOfRangeException">Only null value is supported.</exception>
        /// <exception cref="NotSupportedException">Collection is read-only.</exception>
        public Entry this[string title] {
            get {
                foreach (var entry in this.BaseCollection) {
                    if (entry.Title.Equals(title, StringComparison.CurrentCultureIgnoreCase)) {
                        return entry;
                    }
                }

                if (this.IsReadOnly) { return new Entry(); } //return dummy entry if collection is read-only 

                var newEntry = new Entry(title);
                this.Add(newEntry);
                return newEntry;
            }
            set {
                if (this.IsReadOnly) { throw new NotSupportedException("Collection is read-only."); }
                if (value != null) { throw new ArgumentOutOfRangeException(nameof(value), "Only null value is supported."); }

                Entry entryToRemove = null;
                foreach (var entry in this.BaseCollection) {
                    if (entry.Title.Equals(title, StringComparison.CurrentCultureIgnoreCase)) {
                        entryToRemove = entry;
                        break;
                    }
                }
                if (entryToRemove != null) {
                    this.Remove(entryToRemove);
                }
            }
        }

        /// <summary>
        /// Gets entry with the specified title.
        /// If multiple elements exist with the same title, the first one is returned.
        /// If type does not exist, it is created.
        /// Search is case-insensitive.
        /// 
        /// If value is set to null, entry is removed.
        /// </summary>
        /// <param name="group">Group.</param>
        /// <param name="title">Title.</param>
        /// <exception cref="ArgumentOutOfRangeException">Only null value is supported.</exception>
        /// <exception cref="NotSupportedException">Collection is read-only.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1023:IndexersShouldNotBeMultidimensional", Justification = "Done intentionally for the ease of use.")]
        public Entry this[GroupPath group, string title] {
            get {
                foreach (var entry in this.BaseCollection) {
                    if (entry.Group.Equals(group) && entry.Title.Equals(title, StringComparison.CurrentCultureIgnoreCase)) {
                        return entry;
                    }
                }

                if (this.IsReadOnly) { return new Entry(); } //return dummy entry if collection is read-only 

                var newEntry = new Entry(group, title);
                this.Add(newEntry);
                return newEntry;
            }
            set {
                if (this.IsReadOnly) { throw new NotSupportedException("Collection is read-only."); }
                if (value != null) { throw new ArgumentOutOfRangeException(nameof(value), "Only null value is supported."); }

                Entry entryToRemove = null;
                foreach (var entry in this.BaseCollection) {
                    if (entry.Group.Equals(group) && entry.Title.Equals(title, StringComparison.CurrentCultureIgnoreCase)) {
                        entryToRemove = entry;
                        break;
                    }
                }
                if (entryToRemove != null) {
                    this.Remove(entryToRemove);
                }
            }
        }

        /// <summary>
        /// Gets entry with the specified title.
        /// If multiple elements exist with the same title, the first one is returned.
        /// If type does not exist, it is created.
        /// Search is case-insensitive.
        ///
        /// If value is set to null, record field is removed.
        /// </summary>
        /// <param name="title">Title.</param>
        /// <param name="type">Record type.</param>
        /// <exception cref="ArgumentOutOfRangeException">Only null value is supported.</exception>
        /// <exception cref="NotSupportedException">Collection is read-only.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1023:IndexersShouldNotBeMultidimensional", Justification = "Type represents a logical data store.")]
        public Record this[string title, RecordType type] {
            get {
                var entry = this[title];
                return entry.Records[type];
            }
            set {
                if (this.IsReadOnly) { throw new NotSupportedException("Collection is read-only."); }
                if (value != null) { throw new ArgumentOutOfRangeException(nameof(value), "Only null value is supported."); }

                Entry entryToModify = null;
                foreach (var entry in this.BaseCollection) {
                    if (entry.Title.Equals(title, StringComparison.CurrentCultureIgnoreCase)) {
                        entryToModify = entry;
                        break;
                    }
                }
                if (entryToModify != null) { //to avoid creating entry
                    entryToModify.Records[type] = null;
                }
            }
        }

        /// <summary>
        /// Gets entry with the specified title.
        /// If multiple elements exist with the same title, the first one is returned.
        /// If type does not exist, it is created.
        /// Search is case-insensitive.
        ///
        /// If value is set to null, record field is removed.
        /// </summary>
        /// <param name="group">Group.</param>
        /// <param name="title">Title.</param>
        /// <param name="type">Record type.</param>
        /// <exception cref="ArgumentOutOfRangeException">Only null value is supported.</exception>
        /// <exception cref="NotSupportedException">Collection is read-only.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1023:IndexersShouldNotBeMultidimensional", Justification = "Type represents a logical data store.")]
        public Record this[GroupPath group, string title, RecordType type] {
            get {
                var entry = this[group, title];
                return entry.Records[type];
            }
            set {
                if (this.IsReadOnly) { throw new NotSupportedException("Collection is read-only."); }
                if (value != null) { throw new ArgumentOutOfRangeException(nameof(value), "Only null value is supported."); }

                Entry entryToModify = null;
                foreach (var entry in this.BaseCollection) {
                    if (entry.Group.Equals(group) && entry.Title.Equals(title, StringComparison.CurrentCultureIgnoreCase)) {
                        entryToModify = entry;
                        break;
                    }
                }
                if (entryToModify != null) { //to avoid creating entry
                    entryToModify.Records[type] = null;
                }
            }
        }


        /// <summary>
        /// Sorts all entries according to group and title.
        /// </summary>
        public void Sort() {
            this.BaseCollection.Sort((Entry item1, Entry item2) => {
                var groupValue = string.Compare(item1.Group, item2.Group, StringComparison.CurrentCultureIgnoreCase);
                if (groupValue != 0) {
                    return groupValue;
                } else {
                    return string.Compare(item1.Title, item2.Title, StringComparison.CurrentCultureIgnoreCase);
                }
            });
        }

        #endregion

    }
}
