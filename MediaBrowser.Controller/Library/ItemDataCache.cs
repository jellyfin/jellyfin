using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Library
{
    public class ItemDataCache
    {
        private Dictionary<string, object> Data = new Dictionary<string, object>();

        public void SetValue<T>(BaseItem item, string propertyName, T value)
        {
            Data[GetKey(item, propertyName)] = value;
        }

        public T GetValue<T>(BaseItem item, string propertyName)
        {
            string key = GetKey(item, propertyName);

            if (Data.ContainsKey(key))
            {
                return (T)Data[key];
            }

            return default(T);
        }

        private string GetKey(BaseItem item, string propertyName)
        {
            return item.Id.ToString() + "-" + propertyName;
        }
    }
}
