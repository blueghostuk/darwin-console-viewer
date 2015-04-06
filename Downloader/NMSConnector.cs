using Apache.NMS;
using Apache.NMS.ActiveMQ;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Trainwebsites.Darwin.Downloader
{
    public sealed class NMSConnector
    {
        private readonly CancellationTokenSource _cancellationTokenSource;

        public event EventHandler<byte[]> DataRecieved;

        public NMSConnector(CancellationTokenSource cancellationTokenSource)
        {
            _cancellationTokenSource = cancellationTokenSource;
        }

        private IConnection GetConnection()
        {
            Trace.TraceInformation("Connecting to: {0}", ConfigurationManager.AppSettings["ActiveMQConnectionString"]);
            return new ConnectionFactory(ConfigurationManager.AppSettings["ActiveMQConnectionString"])
                .CreateConnection(ConfigurationManager.AppSettings["Username"], ConfigurationManager.AppSettings["Password"]);
        }

        public void SubscribeToFeeds()
        {
            try
            {
                Subscribe();
            }
            catch (Apache.NMS.NMSException nmsE)
            {
                Trace.TraceError("Exception: {0}", nmsE);
                //TraceHelper.FlushLog();
                ResubscribeMechanism();
            }
            catch (RetryException re)
            {
                Trace.TraceError("Exception: {0}", re);
                ResubscribeMechanism();
            }
            catch (AggregateException ae)
            {
                Trace.TraceError("Exception: {0}", ae);
                ResubscribeMechanism();
            }
        }

        private byte _retries = 0;
        private readonly byte MaxRetries = 5;

        private void ResubscribeMechanism()
        {
            if (_retries > MaxRetries)
            {
                Trace.TraceError("Exceeded retry count of {0}. Quitting", MaxRetries);
                throw new RetryException();
            }
            TimeSpan retryTs = TimeSpan.FromSeconds(5 * _retries);
            Trace.TraceError("Retry attempt no {0} in {1}", _retries, retryTs);
            Thread.Sleep(TimeSpan.FromSeconds(5 * _retries));
            _retries++;
            SubscribeToFeeds();
        }

        private void Subscribe()
        {
            using (IConnection connection = this.GetConnection())
            {
                connection.AcknowledgementMode = AcknowledgementMode.AutoAcknowledge;
                string clientId = ConfigurationManager.AppSettings["ActiveMQDurableClientId"];
                if (!string.IsNullOrEmpty(clientId))
                {
                    connection.ClientId = clientId;
                }
                using (var connectionMonitor = new NMSConnectionMonitor(connection, _cancellationTokenSource, TimeSpan.FromMinutes(3)))
                {
                    connection.Start();

                    Task dataFeedTask = Task.Factory.StartNew(() => GetDarwinData(connection, _cancellationTokenSource.Token, connectionMonitor), _cancellationTokenSource.Token);

                    try
                    {
                        dataFeedTask.Wait(_cancellationTokenSource.Token);
                        if (!connectionMonitor.QuitOk)
                        {
                            Trace.TraceError("Connection Monitor did not quit OK. Retrying Connection");
                            //TraceHelper.FlushLog();
                            throw new RetryException();
                        }
                        _cancellationTokenSource.Cancel();
                        Trace.TraceInformation("Closing connection to: {0}", connection);
                    }
                    catch (OperationCanceledException)
                    {
                        Trace.TraceError("Connection Monitor cancelled");
                        //TraceHelper.FlushLog();
                    }
                }
            }
        }

        private void GetDarwinData(IConnection connection, CancellationToken ct, NMSConnectionMonitor connectionMonitor)
        {
            string darwinQueueName = ConfigurationManager.AppSettings["DarwinQueueName"];
            if (!string.IsNullOrEmpty(darwinQueueName))
            {
                using (ISession session = connection.CreateSession())
                {
                    var queue = session.GetQueue(darwinQueueName);
                    OpenAndWaitConsumer(session, queue, connectionMonitor, this.darwinConsumer_Listener, ct);
                }
            }
        }

        private void OpenAndWaitConsumer(ISession session, IDestination destination, NMSConnectionMonitor connectionMonitor, MessageListener listener, CancellationToken ct)
        {
            using (IMessageConsumer consumer = CreateConsumer(session, destination))
            {
                Trace.TraceInformation("Created consumer to {0}", destination);

                consumer.Listener += listener;
                connectionMonitor.AddMessageConsumer(consumer);
                ct.WaitHandle.WaitOne();
            }
        }

        private class RetryException : Exception { }

        private IMessageConsumer CreateConsumer(ISession session, IDestination destination)
        {
            string subscriberId = ConfigurationManager.AppSettings["ActiveMQDurableSubscriberId"];
            if (destination.IsTopic && !string.IsNullOrEmpty(subscriberId))
                return session.CreateDurableConsumer(destination as ITopic, subscriberId, null, false);
            else
                return session.CreateConsumer(destination);
        }

        private void darwinConsumer_Listener(IMessage message)
        {
            byte[] data = ParseData(message);
            if (data != null && data.Any())
            {
                RaiseDataRecd(data);
            }
        }

        private static byte[] ParseData(IMessage message)
        {
            IBytesMessage bytesMsg = message as IBytesMessage;

            if (bytesMsg == null)
                return null;

            Trace.TraceInformation("[{0}] - Recd Msg for {1}",
                bytesMsg.NMSDestination,
                bytesMsg.NMSTimestamp);

            return bytesMsg.Content;
        }

        private void RaiseDataRecd(byte[] data)
        {
            var eh = this.DataRecieved;
            if (null != eh)
                eh(this, data);
        }

        public void Quit()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }
        }
    }
}
