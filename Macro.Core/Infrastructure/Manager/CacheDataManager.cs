using Dignus.DependencyInjection.Attributes;
using Macro.Models;
using System.Collections.Generic;

namespace Macro.Infrastructure.Manager
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Singleton)]
    public class CacheDataManager
    {
        private ulong _currentIndex;
        private readonly object _indexLock = new object();
        private readonly Dictionary<ulong, EventInfoModel> _indexEventInfoToMap;
        private readonly Dictionary<object, object> _cacheDataToMap = new Dictionary<object, object>();
        public CacheDataManager()
        {
            _indexEventInfoToMap = new Dictionary<ulong, EventInfoModel>();
        }

        public void InitDatas(List<EventInfoModel> eventInfoModels)
        {
            _currentIndex = 0;
            _indexEventInfoToMap.Clear();
            foreach (EventInfoModel eventInfoModel in eventInfoModels)
            {
                InsertIndexEventModel(eventInfoModel);
            }
        }

        public ulong IncreaseIndex()
        {
            lock (_indexLock)
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
            Dictionary<ulong, ulong> previousIndexToNewIndex = new Dictionary<ulong, ulong>();
            AssignNewIndexes(model, previousIndexToNewIndex);
            RemapNextIndexes(model, previousIndexToNewIndex);
            InsertIndexEventModel(model);
        }

        private void AssignNewIndexes(EventInfoModel model, Dictionary<ulong, ulong> previousIndexToNewIndex)
        {
            ulong previousIndex = model.ItemIndex;
            ulong newIndex = IncreaseIndex();
            if (previousIndex > 0)
            {
                previousIndexToNewIndex.TryAdd(previousIndex, newIndex);
            }
            model.ItemIndex = newIndex;
            foreach (EventInfoModel child in model.SubEventItems)
            {
                AssignNewIndexes(child, previousIndexToNewIndex);
            }
        }

        private void RemapNextIndexes(EventInfoModel model, Dictionary<ulong, ulong> previousIndexToNewIndex)
        {
            if (previousIndexToNewIndex.TryGetValue(model.EventToNext, out ulong newNextIndex) == true)
            {
                model.EventToNext = newNextIndex;
            }
            foreach (EventInfoModel child in model.SubEventItems)
            {
                RemapNextIndexes(child, previousIndexToNewIndex);
            }
        }

        private void InsertIndexEventModel(EventInfoModel model)
        {
            if (_indexEventInfoToMap.ContainsKey(model.ItemIndex) == false)
            {
                _indexEventInfoToMap.Add(model.ItemIndex, model);
            }
            if (model.ItemIndex > _currentIndex)
            {
                _currentIndex = model.ItemIndex;
            }
            foreach (EventInfoModel child in model.SubEventItems)
            {
                InsertIndexEventModel(child);
            }
        }

        public void RemoveIndexEventModel(EventInfoModel model)
        {
            _indexEventInfoToMap.Remove(model.ItemIndex);
            foreach (EventInfoModel child in model.SubEventItems)
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
