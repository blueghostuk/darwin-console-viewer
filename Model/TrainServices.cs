using System;
using System.Collections.Concurrent;
using Trainwebsites.Darwin.Model.Xml;

namespace Trainwebsites.Darwin.Model
{
    public static class TrainServices
    {
        private static readonly ConcurrentDictionary<long, ConcurrentDictionary<string, TrainService>> _database = new ConcurrentDictionary<long, ConcurrentDictionary<string, TrainService>>();

        public static void Init()
        {
            DateTime start = DateTime.UtcNow.Date.AddDays(-1);
            DateTime end = start.AddDays(2);
            while (start <= end)
            {
                _database.AddOrUpdate(start.Ticks, new ConcurrentDictionary<string, TrainService>(), (key, value) => value);
                start = start.AddDays(1);
            }
        }

        public static void AddOrUpdate(TS trainService)
        {
            var dict = _database[trainService.ssd.Ticks];
            dict.AddOrUpdate(trainService.rid, new TrainService(trainService), (key, value) => { value.Update(trainService); return value; });
        }

        public static void Deactivate(string rttID)
        {
            foreach (var day in _database.Values)
            {
                TrainService ts;
                if (day.TryGetValue(rttID, out ts))
                {
                    ts.Deactivate();
                    break;
                }
            }
        }
    }
}
