using Dignus.DependencyInjection.Attributes;
using Macro.Models;
using System.Collections.Generic;

namespace Macro.Infrastructure.Manager
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Singleton)]
    public class CacheDataManager
    {
        private ulong _currentIndex;
        public object _lockObj = new object();
        private readonly Dictionary<ulong, EventInfoModel> _indexEventInfoToMap;
        private readonly Dictionary<object, object> _cacheDataToMap = new Dictionary<object, object>();
        public CacheDataManager()
        {
            _indexEventInfoToMap = new Dictionary<ulong, EventInfoModel>();
        }

        public void InitDatas(List<EventInfoModel> eventInfoModels)
        {
            _indexEventInfoToMap.Clear();
            foreach (var item in eventInfoModels)
            {
                _indexEventInfoToMap.Add(item.ItemIndex, item);

                if (item.ItemIndex > _currentIndex)
                {
                    _currentIndex = item.ItemIndex;
                }
            }
        }

        public ulong IncreaseIndex()
        {
            lock (_lockObj)
            {
                _currentIndex++;
            }

            return _currentIndex;
        }

        public EventInfoModel GetEventInfoModel(ulong index)
        {
            _indexEventInfoToMap.TryGetValue(index, out EventInfoModel item);
            return item;
        }

        public void MakeIndexTriggerModel(EventInfoModel model)
        {
            model.ItemIndex = IncreaseIndex();

            foreach (var child in model.SubEventItems)
            {
                MakeIndexTriggerModel(child);
            }
        }

        private void InsertIndexEventModel(EventInfoModel model)
        {
            if (_indexEventInfoToMap.ContainsKey(model.ItemIndex) == false)
            {
                _indexEventInfoToMap.Add(model.ItemIndex, model);
            }
            foreach (var child in model.SubEventItems)
            {
                InsertIndexEventModel(child);
            }
        }

        private void RemoveIndexEventModel(EventInfoModel model)
        {
            if (_indexEventInfoToMap.ContainsKey(model.ItemIndex))
            {
                _indexEventInfoToMap.Remove(model.ItemIndex);
            }
            foreach (var child in model.SubEventItems)
            {
                RemoveIndexEventModel(child);
            }
        }
        public void AddData(object key, object value)
        {
            if (_cacheDataToMap.ContainsKey(key) == false)
            {
                _cacheDataToMap.Add(key, value);
                return;
            }
            _cacheDataToMap[key] = value;
        }
        public object GetData(object key)
        {
            _cacheDataToMap.TryGetValue(key, out object value);

            return value;
        }
        public T GetData<T>(object key)
        {
            if (_cacheDataToMap.TryGetValue(key, out object value) == false)
            {
                return default;
            }
            return (T)value;
        }
        public void DeleteData(object key)
        {
            _cacheDataToMap.Remove(key);
        }
    }
}
