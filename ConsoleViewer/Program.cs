using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Trainwebsites.Darwin.Downloader;
using Trainwebsites.Darwin.Model;
using Trainwebsites.Darwin.Model.Xml;

namespace Trainwebsites.Darwin.ConsoleViewer
{
    class Program
    {
        private static readonly NMSWrapper _nmsConnection = new NMSWrapper();

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Darwin Console Reader");
            Console.WriteLine("Powered by National Rail Enquiries");
            Console.ForegroundColor = ConsoleColor.White;

            TrainServices.Init();

            _nmsConnection.FeedDataRecieved += _nmsConnection_FeedDataRecieved;
            _nmsConnection.Start();

            Console.WriteLine("Press ESC key to exit");
            while (Console.ReadKey().Key != ConsoleKey.Escape)
            {

            }

            _nmsConnection.Stop();
        }

        static void _nmsConnection_FeedDataRecieved(object sender, Pport e)
        {
            switch (e.ItemElementName)
            {
                case ItemChoiceType1.uR:
                    var ur = (DataResponse)e.Item;
                    if (ur.alarm != null && ur.alarm.Any())
                    {

                    }
                    if (ur.association != null && ur.association.Any())
                    {

                    }
                    if (ur.deactivated != null && ur.deactivated.Any())
                    {
                        foreach (var ts in ur.deactivated)
                        {
                            TrainServices.Deactivate(ts.rid);
                        }
                    }
                    if (ur.OW != null && ur.OW.Any())
                    {

                    }
                    if (ur.schedule != null && ur.schedule.Any())
                    {

                    }
                    if (ur.trackingID != null && ur.trackingID.Any())
                    {

                    }
                    if (ur.trainAlert != null && ur.trainAlert.Any())
                    {

                    }
                    if (ur.trainOrder != null && ur.trainOrder.Any())
                    {

                    }
                    if (ur.TS != null && ur.TS.Any())
                    {
                        foreach (var ts in ur.TS)
                        {
                            TrainServices.AddOrUpdate(ts);
                        }
                    }

                    break;

                case ItemChoiceType1.sR:

                    break;
            }
        }
    }
}
