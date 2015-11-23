﻿using SharpAdbClient.Exceptions;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.IO;

namespace SharpAdbClient.Tests
{
    internal class TracingAdbSocket : AdbSocket, IDummyAdbSocket
    {
        public TracingAdbSocket(IPEndPoint endPoint) : base(endPoint)
        {
        }

        public Stream ShellStream
        {
            get;
            set;
        }

        public bool DoDispose
        {
            get;
            set;
        }

        public Queue<AdbResponse> Responses
        {
            get;
        } = new Queue<AdbResponse>();

        public Queue<string> ResponseMessages
        { get; } = new Queue<string>();

        public Queue<SyncCommand> SyncResponses
        {
            get;
        } = new Queue<SyncCommand>();

        public Queue<byte[]> SyncDataReceived
        {
            get;
        } = new Queue<byte[]>();

        public Queue<byte[]> SyncDataSent
        {
            get;
        } = new Queue<byte[]>();

        public List<string> Requests
        { get; } = new List<string>();

        public List<Tuple<SyncCommand, string>> SyncRequests
        { get; } = new List<Tuple<SyncCommand, string>>();

        public override void Dispose()
        {
            if (this.DoDispose)
            {
                base.Dispose();
            }
        }

        public override void Read(byte[] data)
        {
            StackTrace trace = new StackTrace(false);

            base.Read(data);

            if (trace.GetFrame(1).GetMethod().DeclaringType != typeof(AdbSocket))
            {
                this.SyncDataReceived.Enqueue(data);
            }
        }

        public override void Read(byte[] data, int length, int timeout)
        {
            StackTrace trace = new StackTrace(false);

            base.Read(data, length, timeout);

            if (trace.GetFrame(1).GetMethod().DeclaringType != typeof(AdbSocket))
            {
                this.SyncDataReceived.Enqueue(data.Take(length).ToArray());
            }
        }

        public override AdbResponse ReadAdbResponse(bool readDiagString)
        {
            Exception exception = null;
            AdbResponse response;

            try
            {
                response = base.ReadAdbResponse(readDiagString);
            }
            catch (AdbException ex)
            {
                exception = ex;
                response = ex.Response;
            }

            this.Responses.Enqueue(response);

            if (exception != null)
            {
                throw exception;
            }

            return response;
        }

        public override string ReadString()
        {
            var value = base.ReadString();
            this.ResponseMessages.Enqueue(value);
            return value;
        }

        public override string ReadSyncString()
        {
            var value = base.ReadSyncString();
            this.ResponseMessages.Enqueue(value);
            return value;
        }

        public async override Task<string> ReadStringAsync()
        {
            var value = await base.ReadStringAsync();
            this.ResponseMessages.Enqueue(value);
            return value;
        }

        public override void SendAdbRequest(string request)
        {
            this.Requests.Add(request);
            base.SendAdbRequest(request);
        }

        public override void SendSyncRequest(SyncCommand command, string path)
        {
            this.SyncRequests.Add(new Tuple<SyncCommand, string>(command, path));
            base.SendSyncRequest(command, path);
        }

        public override void SendSyncRequest(SyncCommand command, int length)
        {
            StackTrace trace = new StackTrace(false);

            if (trace.GetFrame(1).GetMethod().DeclaringType != typeof(AdbSocket))
            {
                this.SyncRequests.Add(new Tuple<SyncCommand, string>(command, length.ToString()));
            }

            base.SendSyncRequest(command, length);
        }

        public override SyncCommand ReadSyncResponse()
        {
            var response = base.ReadSyncResponse();
            this.SyncResponses.Enqueue(response);
            return response;
        }

        public override void Send(byte[] data, int length, int timeout)
        {
            StackTrace trace = new StackTrace(false);

            base.Send(data, length, timeout);

            if (trace.GetFrame(1).GetMethod().DeclaringType != typeof(AdbSocket))
            {
                this.SyncDataSent.Enqueue(data.Take(length).ToArray());
            }
        }
    }
}
