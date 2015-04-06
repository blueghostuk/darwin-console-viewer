using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Trainwebsites.Darwin.Downloader;
using Trainwebsites.Darwin.Model.Xml;

namespace Trainwebsites.Darwin.ConsoleViewer
{
    internal sealed class NMSWrapper
    {
        private static readonly XmlSerializer _xml = new XmlSerializer(typeof(Pport));
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly NMSConnector _nmsDownloader;

        public event EventHandler<Pport> FeedDataRecieved;

        public NMSWrapper()
        {
            _nmsDownloader = new NMSConnector(_cts);
        }

        public Task Start()
        {
            _nmsDownloader.DataRecieved += (src, feedData) =>
            {
                // run this as a task to return to callee quicker
                Task.Run(() =>
                {
                    using (MemoryStream memStream = new MemoryStream(feedData))
                    using (GZipStream zipStream = new GZipStream(memStream, CompressionMode.Decompress))
                    {
                        var data = _xml.Deserialize(zipStream) as Pport;
                        if (data != null)
                        {
                            var eh = FeedDataRecieved;
                            if (null != eh)
                                eh(this, data);
                        }
                    }
                });
            };

            return Task.Run(() => _nmsDownloader.SubscribeToFeeds());
        }

        public void Stop()
        {
            _nmsDownloader.Quit();
        }
    }
}