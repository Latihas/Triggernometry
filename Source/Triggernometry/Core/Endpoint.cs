using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Triggernometry.Core.Variables;

namespace Triggernometry.Core;

internal class Endpoint : IDisposable {
	internal class Context {
		public HttpListener Listener { get; set; }
		public string Endpoint { get; set; }
		public bool Running { get; set; }
		public Thread CtxThread { get; set; }
	}

	internal enum StatusEnum {
		Unchanged,
		Starting,
		Started,
		Stopping,
		Stopped
	}

	internal static RealPlugin plug => RealPlugin.Instance;
	internal StatusEnum Status { get; set; }
	internal string StatusDescription { get; set; } = "zzz";
	private Context? curctx;
	internal uint ReceivedTelegrams;
	internal List<Tuple<DateTime, string>> teleHistory = [];

	internal delegate void StatusChangeDelegate(StatusEnum newStatus, string statusDesc);

	internal event StatusChangeDelegate? OnStatusChange;

	public Endpoint() {
		SetStatus(StatusEnum.Stopped, null);
	}

	private void SetStatus(StatusEnum st, string? desc) {
		var notify = false;
		if (st != StatusEnum.Unchanged) {
			Status = st;
			notify = true;
		}
		if (desc != null) {
			StatusDescription = $"[{DateTime.Now}] {desc}";
			notify = true;
		}
		if (notify && OnStatusChange != null)
			OnStatusChange(Status, StatusDescription);
	}

	public void Start() {
		try {
			SetStatus(StatusEnum.Starting, null);
			var http = new HttpListener();
			http.Prefixes.Clear();
			http.Prefixes.Add(plug.cfg.HttpEndpoint);
			lock (plug.cfg.Constants) {
				plug.cfg.Constants["TriggernometryEndpoint"] = new VariableScalar {
					Value = plug.cfg.HttpEndpoint
				};
			}
			var th = new Thread(ThreadProc);
			var ctx = new Context {
				Endpoint = plug.cfg.HttpEndpoint,
				Running = true,
				CtxThread = th,
				Listener = http
			};
			lock (this) {
				if (curctx != null) {
					curctx.Running = false;
					curctx.Listener.Abort();
				}
				curctx = ctx;
			}
			ReceivedTelegrams = 0;
			http.Start();
			th.Name = "Telesto endpoint";
			th.Start(ctx);
		} catch (Exception ex) {
			Stop();
			SetStatus(StatusEnum.Unchanged, $"Exception on Start: {ex.Message} @ {ex.StackTrace}");
		}
	}

	public void Stop() {
		SetStatus(StatusEnum.Stopping, null);
		lock (this) {
			if (curctx != null) {
				curctx.Running = false;
				curctx.Listener.Abort();
			}
			curctx = null;
		}
		SetStatus(StatusEnum.Stopped, null);
	}

	public void Dispose() => Stop();

	public void ThreadProc(object? o) {
		var ctx = (Context)o!;
		var http = ctx.Listener;
		SetStatus(StatusEnum.Started, $"Waiting for connections on {ctx.Endpoint}");
		while (ctx.Running && http.IsListening) {
			try {
				var hctx = http.GetContext();
				var t = new Task(() => {
					try {
						var req = hctx.Request;
						if (req.HttpMethod != "POST") {
							throw new InvalidOperationException("Received request was not HTTP POST");
						}
						string body;
						using (var sr = new StreamReader(req.InputStream, req.ContentEncoding)) {
							body = sr.ReadToEnd();
						}
						ReceivedTelegrams++;
						lock (teleHistory) {
							teleHistory.Add(new Tuple<DateTime, string>(DateTime.Now, body));
							if (teleHistory.Count > 100) {
								teleHistory.RemoveAt(0);
							}
						}
						plug.EndpointReceive(body);
						hctx.Response.StatusCode = 200;
					} catch (Exception ex) {
						hctx.Response.StatusCode = 500;
						SetStatus(StatusEnum.Unchanged, $"Exception in Task: {ex.Message} @ {ex.StackTrace}");
					}
					hctx.Response.Close();
				});
				t.Start();
			} catch (Exception ex) {
				SetStatus(StatusEnum.Unchanged, $"Exception in ThreadProc: {ex.Message} @ {ex.StackTrace}");
			}
		}
		SetStatus(StatusEnum.Unchanged, "Thread exited");
	}
}