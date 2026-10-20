using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace Gambler.Bot.Core.SiteClient
{
    /// <summary>
    /// An <see cref="IDictionary{String,String}"/> view over an <see cref="HttpHeaders"/>
    /// collection, so that <c>ISiteClient.DefaultHeaders</c> can be used dictionary-style while
    /// actually mutating the underlying client's default request headers.
    /// </summary>
    public class HttpHeadersDictionary : IDictionary<string, string>
    {
        private readonly HttpHeaders _headers;

        public HttpHeadersDictionary(HttpHeaders headers)
        {
            _headers = headers;
        }

        public ICollection<string> Keys
        {
            get
            {
                var list = new List<string>();
                foreach (var header in _headers)
                    list.Add(header.Key);
                return list;
            }
        }

        public ICollection<string> Values
        {
            get
            {
                var list = new List<string>();
                foreach (var header in _headers)
                {
                    foreach (var value in header.Value)
                        list.Add(value);
                }
                return list;
            }
        }

        public int Count
        {
            get
            {
                int count = 0;
                foreach (var header in _headers)
                {
                    foreach (var _ in header.Value)
                        count++;
                }
                return count;
            }
        }

        public bool IsReadOnly => false;

        public string this[string key]
        {
            get
            {
                var values = _headers.GetValues(key);
                if (values == null)
                    return null;
                foreach (var value in values)
                    return value;
                return null;
            }
            set
            {
                _headers.Remove(key);
                _headers.TryAddWithoutValidation(key, value);
            }
        }

        public void Add(string key, string value)
        {
            _headers.TryAddWithoutValidation(key, value);
        }

        public void Add(KeyValuePair<string, string> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            foreach (var key in Keys)
                _headers.Remove(key);
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            if (!_headers.Contains(item.Key))
                return false;
            var values = _headers.GetValues(item.Key);
            if (values == null)
                return false;
            foreach (var value in values)
                if (value == item.Value)
                    return true;
            return false;
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            int i = arrayIndex;
            foreach (var header in _headers)
            {
                foreach (var value in header.Value)
                {
                    array[i++] = new KeyValuePair<string, string>(header.Key, value);
                }
            }
        }

        public bool ContainsKey(string key) => _headers.Contains(key);

        public bool Remove(string key) => _headers.Remove(key);

        public bool Remove(KeyValuePair<string, string> item)
        {
            if (Contains(item))
            {
                if (_headers.Contains(item.Key))
                {
                    var values = _headers.GetValues(item.Key);
                    var remaining = new List<string>();
                    if (values != null)
                    {
                        foreach (var value in values)
                        {
                            if (value != item.Value)
                                remaining.Add(value);
                        }
                    }
                    _headers.Remove(item.Key);
                    foreach (var value in remaining)
                        _headers.TryAddWithoutValidation(item.Key, value);
                    return true;
                }
            }
            return false;
        }

        public bool TryGetValue(string key, out string value)
        {
            var values = _headers.GetValues(key);
            if (values != null)
            {
                foreach (var v in values)
                {
                    value = v;
                    return true;
                }
            }
            value = null;
            return false;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach (var header in _headers)
            {
                foreach (var value in header.Value)
                {
                    yield return new KeyValuePair<string, string>(header.Key, value);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
