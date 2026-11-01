using Dignus.DependencyInjection.Attributes;
using Macro.Infrastructure.Manager;
using Macro.Models;
using Macro.Models.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using Utils;

namespace Macro.Infrastructure.Services
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Singleton)]
    public class EventEditingService
    {
        private readonly Config _config;
        private readonly FileService _fileService;
        private readonly CacheDataManager _cacheDataManager;

        public ObservableCollection<EventInfoModel> EventItems { get; } = new();

        public EventEditingService(Config config, FileService fileService, CacheDataManager cacheDataManager)
        {
            _config = config;
            _fileService = fileService;
            _cacheDataManager = cacheDataManager;
        }

        public void Load(string path)
        {
            ConfigureImageDirectory();
            List<EventInfoModel> loadedEvents;
            if (_fileService.LooksLikeJson(path) == true)
            {
                loadedEvents = _fileService.LoadSaveAsJson(path) ?? new List<EventInfoModel>();
            }
            else
            {
                loadedEvents = EventModelConverter.ToEventInfoList(_fileService.Load<EventTriggerModel>(path));
            }
            List<EventInfoModel> previousEvents = EventItems.ToList();
            _cacheDataManager.InitDatas(loadedEvents);
            EventItems.Clear();
            foreach (EventInfoModel eventInfo in loadedEvents)
            {
                EventItems.Add(eventInfo);
            }
            foreach (EventInfoModel previousEvent in previousEvents)
            {
                previousEvent.Dispose();
            }
        }

        public bool Save()
        {
            ConfigureImageDirectory();
            return _fileService.SaveAsJson(Path.Combine(GetSaveDirectory(), ConstHelper.DefaultSaveFileName), EventItems.ToList());
        }

        private string GetSaveDirectory()
        {
            if (string.IsNullOrEmpty(_config.SavePath) == true)
            {
                return ConstHelper.DefaultSavePath;
            }
            return _config.SavePath;
        }

        private void ConfigureImageDirectory()
        {
            JsonHelper.ConfigureImageDirectory(Path.Combine(GetSaveDirectory(), "img"));
        }

        public EventInfoModel Find(ulong eventId)
        {
            EventInfoModel eventInfo = _cacheDataManager.GetEventInfoModel(eventId);
            if (eventInfo == null)
            {
                throw new ArgumentException("The event does not exist: " + eventId);
            }
            return eventInfo;
        }

        public void Add(EventInfoModel eventInfo, ulong parentId = 0)
        {
            ObservableCollection<EventInfoModel> destination = EventItems;
            if (parentId != 0)
            {
                destination = Find(parentId).SubEventItems;
            }
            _cacheDataManager.MakeIndexTriggerModel(eventInfo);
            destination.Add(eventInfo);
        }

        public void Remove(EventInfoModel eventInfo)
        {
            ObservableCollection<EventInfoModel> source = FindContainingCollection(EventItems, eventInfo);
            if (source == null)
            {
                throw new ArgumentException("The event is not in the current configuration.");
            }
            source.Remove(eventInfo);
            _cacheDataManager.RemoveIndexEventModel(eventInfo);
            eventInfo.Dispose();
        }

        public void Move(EventInfoModel eventInfo, ulong parentId, int position = -1)
        {
            ObservableCollection<EventInfoModel> destination = EventItems;
            if (parentId != 0)
            {
                EventInfoModel parent = Find(parentId);
                if (FindContainingCollection(eventInfo.SubEventItems, parent) != null)
                {
                    throw new ArgumentException("An event cannot be moved under its descendant.");
                }
                if (ReferenceEquals(eventInfo, parent) == true)
                {
                    throw new ArgumentException("An event cannot be its own parent.");
                }
                destination = parent.SubEventItems;
            }
            ObservableCollection<EventInfoModel> source = FindContainingCollection(EventItems, eventInfo);
            if (source == null)
            {
                throw new ArgumentException("The event is not in the current configuration.");
            }
            int destinationCount = destination.Count;
            if (ReferenceEquals(source, destination) == true)
            {
                destinationCount--;
            }
            if (position == -1)
            {
                position = destinationCount;
            }
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }
            if (position > destinationCount)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }
            if (ReferenceEquals(source, destination) == true)
            {
                source.Move(source.IndexOf(eventInfo), position);
                return;
            }
            source.Remove(eventInfo);
            destination.Insert(position, eventInfo);
        }

        private ObservableCollection<EventInfoModel> FindContainingCollection(ObservableCollection<EventInfoModel> events, EventInfoModel eventInfo)
        {
            if (events.Contains(eventInfo) == true)
            {
                return events;
            }
            foreach (EventInfoModel parent in events)
            {
                ObservableCollection<EventInfoModel> result = FindContainingCollection(parent.SubEventItems, eventInfo);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        public void Update(EventInfoModel eventInfo, EventInfoModel replacement)
        {
            Bitmap replacementImage = new(replacement.Image);
            Bitmap previousImage = eventInfo.Image;
            eventInfo.Image = replacementImage;
            previousImage?.Dispose();
            eventInfo.EventType = replacement.EventType;
            eventInfo.MouseEventInfo = replacement.MouseEventInfo;
            eventInfo.KeyboardCmd = replacement.KeyboardCmd;
            eventInfo.ProcessInfo = replacement.ProcessInfo;
            eventInfo.MonitorInfo = replacement.MonitorInfo;
            eventInfo.AfterDelay = replacement.AfterDelay;
            eventInfo.RepeatInfo = replacement.RepeatInfo;
            eventInfo.EventToNext = replacement.EventToNext;
            eventInfo.SameImageDrag = replacement.SameImageDrag;
            eventInfo.MaxDragCount = replacement.MaxDragCount;
            eventInfo.HardClick = replacement.HardClick;
            eventInfo.RoiDataInfo = replacement.RoiDataInfo;
            eventInfo.IsChecked = replacement.IsChecked;
            eventInfo.Similarity = replacement.Similarity;
            eventInfo.PositionRelativeToImage = replacement.PositionRelativeToImage;
        }
    }
}
