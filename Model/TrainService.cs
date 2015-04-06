using System;
using Trainwebsites.Darwin.Model.Xml;

namespace Trainwebsites.Darwin.Model
{
    public class TrainService : IEquatable<TrainService>
    {
        public string UID { get; set; }

        public string RttID { get; set; }

        public TrainService(TS trainService)
        {
            this.UID = trainService.uid;
            this.RttID = trainService.rid;
        }

        public void Update(TS trainService)
        {

        }

        public bool Equals(TrainService other)
        {
            return other.RttID == this.RttID;
        }

        public void Deactivate()
        {
            // TODO: implement
        }
    }
}
