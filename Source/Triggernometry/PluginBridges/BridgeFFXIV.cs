using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Advanced_Combat_Tracker;
using FFXIV_ACT_Plugin.Common.Models;
using Triggernometry.Core;
using Triggernometry.Core.Variables;
using Triggernometry.FFXIV;
using Triggernometry.Localization;
using TriggernometryProxy;

namespace Triggernometry.PluginBridges;

public static class BridgeFFXIV {
	public static Configuration cfg = RealPlugin.Instance.cfg;

	internal delegate void LoggingDelegate(RealPlugin.DebugLevelEnum level, string text);

	internal static event LoggingDelegate OnLogEvent;

	public static long LastCheck;
	public static long TickNum;
	public static uint ZoneID => ProxyPlugin.ClientState.TerritoryType;

	static BridgeFFXIV() {
		SetupNullCombatant();
	}


	public static RealPlugin.PluginWrapper GetWrappedPlugin() => new() {
		pluginObj = ActGlobals.oFormActMain.FfxivPlugin
	};

	private static FFXIV_ACT_Plugin.FFXIV_ACT_Plugin? _FFXIV_ACT_Plugin_Instance;

	public static FFXIV_ACT_Plugin.FFXIV_ACT_Plugin GetInstance() {
		_FFXIV_ACT_Plugin_Instance ??= ActGlobals.oFormActMain.FfxivPlugin;
		return _FFXIV_ACT_Plugin_Instance;
	}

	public static Process GetProcess() => GetInstance().DataRepository.GetCurrentFFXIVProcess();

	public static int GetProcessId() => GetProcess().Id;

	public static string GetProcessName() => GetProcess().ProcessName;

	public static string GetGameVersion() => GetInstance().DataRepository.GetGameVersion();

	public static void SubscribeToZoneChanged(RealPlugin p) =>
		GetInstance().DataSubscription.ZoneChanged += p.ZoneChangeDelegate;

	public static void UnsubscribeFromNetworkEvents(RealPlugin p) {
		// GetInstance().DataSubscription.ParsedLogLine -= p.NetworkLogLineReceiver;
	}

	private static void LogMessage(RealPlugin.DebugLevelEnum level, string message) => OnLogEvent?.Invoke(level, message);

	#region Actions

	// public static CheckBox chkLogAllNetwork => (CheckBox)ScanControl(GetWrappedPlugin()?.TabPage, "chkLogAllNetwork");
	// public static CheckBox chkUseDeucalion => (CheckBox)ScanControl(GetWrappedPlugin()?.TabPage, "chkUseDeucalion");

	public static void UseDeucalion(bool enabled) {
		// if (chkUseDeucalion.InvokeRequired)
		// {
		//     chkUseDeucalion.Invoke(new System.Action(() => chkUseDeucalion.Checked = enabled));
		// }
		// else
		// {
		//     chkUseDeucalion.Checked = enabled;
		// }
	}

	public static void LogAllNetwork(bool enabled) {
		// if (chkLogAllNetwork.InvokeRequired)
		// {
		//     chkLogAllNetwork.Invoke(new System.Action(() => chkLogAllNetwork.Checked = enabled));
		// }
		// else
		// {
		//     chkLogAllNetwork.Checked = enabled;
		// }
	}

	#endregion Actions

	#region Combatants

	internal static VariableDictionary _nullCombatant = new();
	public static VariableDictionary NullCombatant => (VariableDictionary)_nullCombatant.Duplicate(); // for scripts

	public static uint PlayerId;
	public static string PlayerHexId = "";
	public static VariableDictionary Myself;

	public static int NumPartyMembers;
	public static int PrevNumPartyMembers;
	public static List<VariableDictionary> PartyMembers = [
		new(),
		new(),
		new(),
		new(),
		new(),
		new(),
		new(),
		new()
	];

	public static void ClearCombatant(VariableDictionary vc) {
		vc.SetValue("name", "");
		vc.SetValue("currenthp", "");
		vc.SetValue("currentmp", "");
		vc.SetValue("currentgp", "");
		vc.SetValue("currentcp", "");
		vc.SetValue("maxhp", "");
		vc.SetValue("maxmp", "");
		vc.SetValue("maxgp", "");
		vc.SetValue("maxcp", "");
		vc.SetValue("level", "");
		vc.SetValue("x", "");
		vc.SetValue("y", "");
		vc.SetValue("z", "");
		vc.SetValue("h", "");
		vc.SetValue("id", "");
		vc.SetValue("inparty", "");
		vc.SetValue("inalliance", "");
		vc.SetValue("order", "");
		vc.SetValue("casttargetid", "");
		vc.SetValue("targetid", "");
		vc.SetValue("heading", "");
		vc.SetValue("distance", "");
		vc.SetValue("worldid", "");
		vc.SetValue("worldname", "");
		vc.SetValue("currentworldid", "");
		vc.SetValue("bnpcid", "");
		vc.SetValue("bnpcnameid", "");
		vc.SetValue("ownerid", "");
		vc.SetValue("type", "");
		vc.SetValue("iscasting", "");
		vc.SetValue("castid", "");
		vc.SetValue("casttime", "");
		vc.SetValue("maxcasttime", "");
		vc.SetValue("partytype", "");
		vc.SetValue("address", "");
		foreach (var propName in Job.LegalJobPropNames) {
			vc.SetValue(propName.ToLower(), Job.EmptyJob.QueryProperty(propName)); // role, job, jobid, etc.
		}
	}

	public static void SetupNullCombatant() {
		ClearCombatant(_nullCombatant);
	}

	internal static string ConvertToHex(long x) => x.ToString("X8");

	public static void PopulateClumpFromCombatant(VariableDictionary vc, Combatant? cmx, int inParty, int inAlliance, int orderNum) {
		if (cmx?.Name == null) {
			ClearCombatant(vc);
			return;
		}
		vc.SetValue("name", cmx.Name);
		vc.SetValue("currenthp", cmx.CurrentHP);
		vc.SetValue("currentmp", cmx.CurrentMP);
		vc.SetValue("currentgp", cmx.CurrentGP);
		vc.SetValue("currentcp", cmx.CurrentCP);
		vc.SetValue("maxhp", cmx.MaxHP);
		vc.SetValue("maxmp", cmx.MaxMP);
		vc.SetValue("maxgp", cmx.MaxGP);
		vc.SetValue("maxcp", cmx.MaxCP);
		vc.SetValue("level", cmx.Level);
		vc.SetValue("x", cmx.PosX);
		vc.SetValue("y", cmx.PosY);
		vc.SetValue("z", cmx.PosZ);
		vc.SetValue("id", ConvertToHex(cmx.ID));
		vc.SetValue("inparty", inParty);
		vc.SetValue("inalliance", inAlliance);
		vc.SetValue("order", orderNum);
		if (cmx.IsCasting) vc.SetValue("casttargetid", ConvertToHex(cmx.CastTargetID));
		else vc.SetValue("casttargetid", 0);
		if (cmx.TargetID > 0) vc.SetValue("targetid", ConvertToHex(cmx.TargetID));
		else vc.SetValue("targetid", 0);
		vc.SetValue("iscasting", Convert.ToInt32(cmx.IsCasting));
		vc.SetValue("casttime", cmx.CastDurationCurrent);
		vc.SetValue("maxcasttime", cmx.CastDurationMax);
		if (cmx.CastBuffID > 0) vc.SetValue("castid", cmx.CastBuffID.ToString("X"));
		else vc.SetValue("castid", 0);
		vc.SetValue("heading", cmx.Heading);
		vc.SetValue("h", cmx.Heading);
		vc.SetValue("distance", cmx.EffectiveDistance);
		vc.SetValue("worldid", cmx.WorldID);
		vc.SetValue("worldname", cmx.WorldName);
		vc.SetValue("currentworldid", cmx.CurrentWorldID);
		vc.SetValue("homeworldid", cmx.WorldID);
		vc.SetValue("homeworldname", cmx.WorldName);
		if (cmx.OwnerID > 0) vc.SetValue("ownerid", ConvertToHex(cmx.OwnerID));
		else vc.SetValue("ownerid", 0);
		vc.SetValue("bnpcnameid", cmx.BNpcNameID);
		vc.SetValue("bnpcid", cmx.BNpcID);
		vc.SetValue("partytype", cmx.PartyType.ToString());
		vc.SetValue("address", $"{cmx.Address}"); // IntPtr
		var job = Job.GetJob(cmx.Job);
		foreach (var propName in Job.LegalJobPropNames)
			vc.SetValue(propName.ToLower(), job.QueryProperty(propName)); // role, job, jobid, etc.
	}

	private class CombatantData {
		public object Lock { get; set; }
		public ReadOnlyCollection<Combatant> Combatants { get; set; }
	}

	private static CombatantData GetCombatants() {
		// use DataRepository
		var o = GetInstance().DataRepository;
		PlayerId = o.GetCurrentPlayerID();
		PlayerHexId = ConvertToHex(PlayerId);
		var cd = new CombatantData { Combatants = o.GetCombatantList() };
		cd.Lock = cd;
		return cd;
	}

	public static void UpdateState() {
		var phase = 0;
		try {
			var old = Interlocked.Read(ref LastCheck);
			var now = DateTime.Now.Ticks;
			if ((now - old) / TimeSpan.TicksPerMillisecond < 500) return;
			Interlocked.Exchange(ref LastCheck, now);
			phase = 99;
			var cd = GetCombatants();
			phase = 3;
			lock (cd.Lock) {
				var ex = 0;
				foreach (var cmx in cd.Combatants) {
					int nump;
					try {
						nump = (int)cmx.PartyType;
					} catch (Exception) {
						nump = 0;
					}
					if (cmx.ID == PlayerId || nump == 1) {
						if (ex >= PartyMembers.Count) {
							throw new InvalidOperationException(I18n.Translate("internal/ffxiv/partytoobig", "Party structure has more than {0} members", PartyMembers.Count));
						}
						phase = 4;
						if (cmx.ID == PlayerId) {
							Myself = PartyMembers[ex];
						}
						phase = 5;
						PopulateClumpFromCombatant(PartyMembers[ex], cmx, 1, nump == 2 ? 1 : 0, ex + 1);
						phase = 6;
						for (var i = 0; i < ex; i++) {
							if (PartyMembers[ex].CompareTo(PartyMembers[i]) == 0) {
								ex--;
								break;
							}
						}
						ex++;
						if (ex >= PartyMembers.Count) {
							// full party found
							break;
						}
					}
				}
				phase = 7;
				NumPartyMembers = ex;
				if (PrevNumPartyMembers > NumPartyMembers) {
					for (var i = NumPartyMembers; i < PrevNumPartyMembers; i++) {
						ClearCombatant(PartyMembers[i]);
					}
				}
				PrevNumPartyMembers = NumPartyMembers;
				phase = 8;
				if (cfg.FfxivPartyOrdering == Configuration.FfxivPartyOrderingEnum.CustomSelfFirst) {
					//DebugPlayerSorting("a1", PartyMembers);
					PartyMembers.Sort(SortPlayersSelf);
					var ro = 1;
					foreach (var vc in PartyMembers) {
						vc.SetValue("order", "" + ro);
						ro++;
					}
					//DebugPlayerSorting("a2", PartyMembers);
				} else if (cfg.FfxivPartyOrdering == Configuration.FfxivPartyOrderingEnum.CustomFull) {
					//DebugPlayerSorting("b1", PartyMembers);
					PartyMembers.Sort(SortPlayers);
					var ro = 1;
					foreach (var vc in PartyMembers) {
						vc.SetValue("order", "" + ro);
						ro++;
					}
					//DebugPlayerSorting("b2", PartyMembers);
				}
			}
		} catch (Exception ex) {
			LogMessage(RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/ffxiv/updateexception", "Exception in FFXIV state update: {0} at stage {1}", ex.Message, phase));
		}
	}

	/*private static void DebugPlayerSorting(string header, IEnumerable<VariableClump> vc)
	{
	    int ro = 1;
	    foreach (VariableClump a in vc)
	    {
	        System.Diagnostics.Debug.WriteLine(header + ": " + ro + " -- " + a.GetValue("name") + ", " + a.GetValue("job") + " --> " + a.GetValue("order") + " / " + cfg.GetPartyOrderValue(a.GetValue("jobid")));
	        ro++;
	    }
	}*/

	public static int SortPlayersSelf(VariableDictionary a, VariableDictionary b) {
		if (a == Myself && b != Myself) {
			//System.Diagnostics.Debug.WriteLine(a.GetValue("name") + " (ME) < " + b.GetValue("name"));
			return -1;
		}
		if (b == Myself && a != Myself) {
			//System.Diagnostics.Debug.WriteLine(a.GetValue("name") + " > " + b.GetValue("name") + " (ME)");
			return 1;
		}
		return SortPlayers(a, b);
	}

	public static int SortPlayers(VariableDictionary a, VariableDictionary b) {
		var av = cfg.GetPartyOrderValue(a.GetValue("jobid").ToString());
		var bv = cfg.GetPartyOrderValue(b.GetValue("jobid").ToString());
		if (av < bv) {
			//System.Diagnostics.Debug.WriteLine(a.GetValue("name") + " (" + av + ") < " + b.GetValue("name") + " (" + bv + ")");
			return -1;
		}
		if (av > bv) {
			//System.Diagnostics.Debug.WriteLine(a.GetValue("name") + " (" + av + ") > " + b.GetValue("name") + " (" + bv + ")");
			return 1;
		}
		//System.Diagnostics.Debug.WriteLine(a.GetValue("name") + " (" + av + ") -(" + a.GetValue("name").CompareTo(b.GetValue("name")) + ")- " + b.GetValue("name") + " (" + bv + ")");
		// https://github.com/paissaheavyindustries/Triggernometry/issues/9
		return b.GetValue("id").CompareTo(a.GetValue("id"));
	}

	public static VariableDictionary GetNamedEntity(string name) {
		try {
			var cd = GetCombatants();
			lock (cd.Lock) {
				foreach (var cmx in cd.Combatants) {
					if (cmx.Name == name) {
						var nump = 0;
						try {
							nump = (int)cmx.PartyType;
						} catch (Exception) {
						}
						var vc = new VariableDictionary();
						PopulateClumpFromCombatant(vc, cmx, nump == 1 ? 1 : 0, nump == 2 ? 1 : 0, 0);
						return vc;
					}
				}
			}
		} catch (Exception ex) {
			LogMessage(RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/ffxiv/namedexception", "Exception in FFXIV named entity retrieve: {0}", ex.Message));
		}
		return _nullCombatant;
	}

	public static VariableDictionary GetIdEntity(string id) {
		try {
			var cd = GetCombatants();
			lock (cd.Lock) {
				foreach (var cmx in cd.Combatants) {
					if (string.Compare(ConvertToHex(cmx.ID), id, true) == 0) {
						var nump = 0;
						try {
							nump = (int)cmx.PartyType;
						} catch (Exception) {
						}
						var vc = new VariableDictionary();
						PopulateClumpFromCombatant(vc, cmx, nump == 1 ? 1 : 0, nump == 2 ? 1 : 0, 0);
						return vc;
					}
				}
			}
		} catch (Exception ex) {
			LogMessage(RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/ffxiv/idexception", "Exception in FFXIV ID entity retrieve: {0}", ex.Message));
		}
		return _nullCombatant;
	}

	public static List<VariableDictionary> GetAllEntities() {
		var allEntities = new List<VariableDictionary>();
		try {
			var cd = GetCombatants();
			lock (cd.Lock) {
				foreach (var cmx in cd.Combatants) {
					var nump = 0;
					try {
						nump = (int)cmx.PartyType;
					} catch {
					}

					var vc = new VariableDictionary();
					try {
						PopulateClumpFromCombatant(vc, cmx, nump == 1 ? 1 : 0, nump == 2 ? 1 : 0, 0);
						allEntities.Add(vc);
					} catch (Exception ex) {
						// some NPC entities do not follow the same memory struct with ordinary combatants.
						// the wrongly parsed properties could cause errors.
						LogMessage(RealPlugin.DebugLevelEnum.Warning, I18n.Translate("internal/ffxiv/allentitiesexceptionsingle",
							"Failed to get entity data: name = {0}, id = {1}. Exception: {2}",
							cmx.Name, ConvertToHex(cmx.ID), ex.Message));
					}
				}
			}
		} catch (Exception ex) {
			LogMessage(RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/ffxiv/allentitiesexception", "Exception in FFXIV all entities retrieve: {0}", ex.Message));
		}
		return allEntities;
	}

	public class XivEntity : Entity {
		private readonly Combatant _entity; // the original combatant object from FFXIV_ACT_Plugin, properties change over time
		public override PluginSource PluginSource { get; set; } = PluginSource.XivPlugin;
		public override IntPtr Address => _entity.Address;
		public override string Name => _entity.Name;
		public override uint ID => _entity.ID;
		public override uint BNpcID => _entity.BNpcID;
		public override uint OwnerID => _entity.OwnerID;
		public override EntityType Type => (EntityType)_entity.type; // actually only 1 (PC) or 2 (BattleNpc)
		public override byte EffectiveDistance => _entity.EffectiveDistance;
		public override float PosX => _entity.PosX;
		public override float PosY => _entity.PosY;
		public override float PosZ => _entity.PosZ;
		public override float Heading => _entity.Heading;
		public override uint CurrentHP => _entity.CurrentHP;
		public override uint MaxHP => _entity.MaxHP;
		public override uint CurrentMP => _entity.CurrentMP;
		public override uint MaxMP => _entity.MaxMP;
		public override ushort CurrentCP => (ushort)_entity.CurrentCP; // uint
		public override ushort MaxCP => (ushort)_entity.MaxCP; // uint
		public override ushort CurrentGP => (ushort)_entity.CurrentGP; // uint
		public override ushort MaxGP => (ushort)_entity.MaxGP; // uint
		public override Job Job => Job.TryGetJob(_entity.Job /*int*/, out var result) ? result : Job.GetJob(0);
		public override byte Level => (byte)_entity.Level; // int
		//public override bool InCombat { get; set; }
		public override bool InParty => (int)_entity.PartyType == 1;
		public override bool InAlliance => (int)_entity.PartyType == 2;
		public override uint TargetID => _entity.TargetID;
		public override uint BNpcNameID => _entity.BNpcNameID;
		public override ushort CurrentWorldID => (ushort)_entity.CurrentWorldID; // uint
		public override ushort WorldID => (ushort)_entity.WorldID; // uint
		public override List<Status> Statuses {
			get {
				return _entity.NetworkBuffs
					.Where(rawBuff => (rawBuff?.BuffID ?? 0) != 0)
					.Select(Status (rawBuff) => new XivStatus(rawBuff, this))
					.ToList();
			}
		}
		public override bool IsCasting => _entity.IsCasting;
		public override uint CastID => _entity.CastBuffID;
		public override uint CastTargetID => _entity.CastTargetID;
		public override float CastTime => _entity.CastDurationCurrent;
		public override float MaxCastTime => _entity.CastDurationMax;

		internal XivEntity() {
		}

		internal XivEntity(Combatant xivEntity) {
			_entity = xivEntity;
		}

		internal new static Entity NullEntity() => new() {
			Exist = false,
			PluginSource = PluginSource.XivPlugin
		};

		/* example:
		class Combatant
		Fields:
		  NetworkBuffs : NetworkBuff[] = FFXIV_ACT_Plugin.Common.Models.NetworkBuff[];
		Properties:
		  ID : UInt32 = 1073743259;
		  OwnerID : UInt32 = 0;
		  type : Byte = 2;
		  Job : Int32 = 0;
		  Level : Int32 = 80;
		  Name : String = Striking Dummy;
		  CurrentHP : UInt32 = 2134350;
		  MaxHP : UInt32 = 2134350;
		  CurrentMP : UInt32 = 0;
		  MaxMP : UInt32 = 10000;
		  CurrentCP : UInt32 = 0;
		  MaxCP : UInt32 = 0;
		  CurrentGP : UInt32 = 0;
		  MaxGP : UInt32 = 0;
		  IsCasting : Boolean = False;
		  CastBuffID : UInt32 = 0;
		  CastTargetID : UInt32 = 3758096384;
		  CastDurationCurrent : Single = 0;
		  CastDurationMax : Single = 0;
		  PosX : Single = 510.3607;
		  PosY : Single = -392.0923;
		  PosZ : Single = 167.9883;
		  Heading : Single = -2.460303;
		  CurrentWorldID : UInt32 = 0;
		  WorldID : UInt32 = 0;
		  WorldName : String = ;
		  BNpcNameID : UInt32 = 541;
		  BNpcID : UInt32 = 13728;
		  TargetID : UInt32 = 0;
		  EffectiveDistance : Byte = 81;
		  PartyType : PartyType = None;
		  Address : IntPtr = 2079081754992;
		  Order : Int32 = 10;
		 */
	}

	public class XivStatus : Status {
		public override PluginSource PluginSource { get; set; } = PluginSource.XivPlugin;

		private readonly NetworkBuff _networkBuff;
		public override ushort StatusID => _networkBuff.BuffID;
		public override ushort Stack => _networkBuff.BuffExtra;
		private DateTime Timestamp => _networkBuff.Timestamp;
		private float Duration => _networkBuff.Duration;
		public override float Timer => Duration - (float)(DateTime.Now - Timestamp).TotalSeconds;
		public override uint SourceID => _networkBuff.ActorID;

		private readonly Entity _target;
		public override Entity Target => _target;

		public XivStatus(NetworkBuff networkBuff, Entity target) {
			_networkBuff = networkBuff;
			_target = target;
		}

		/* example:
		class NetworkBuff
		Properties:
		  BuffID : ushort = 1191;
		  BuffExtra : ushort = 0;
		  Timestamp : DateTime = 2024/12/26 16:40:36;
		  Duration : float = 20;
		  ActorID : uint = 277654321;
		  ActorName : string = My Name;
		  TargetID : uint = 277654321;
		  TargetName : String = My Name;
		  RefreshPending : Boolean = False;
		 */
	}

	internal static IEnumerable<Entity> InternalGetEntities() {
		try {
			var cd = GetCombatants();
			var combatants = cd.Combatants;
			return combatants.Select(Entity (c) => new XivEntity(c));
		} catch (Exception ex) {
			LogMessage(RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/ffxiv/allentitiesexception",
				"Exception in FFXIV all entities retrieve: {0}", ex.Message));
		}
		return [];
	}

	/// <returns>XivEntity.NullEntity() if not found.</returns>
	internal static Entity InternalGetEntityByID(uint id) {
		return InternalGetEntities().FirstOrDefault(entity => entity.ID == id) ?? XivEntity.NullEntity();
	}

	/// <returns>XivEntity.NullEntity() if not found.</returns>
	internal static Entity InternalGetMyself() => InternalGetEntities().FirstOrDefault() ?? XivEntity.NullEntity();

	public static VariableDictionary GetPartyMember(int index) {
		UpdateState();
		if (index < 1 || index > NumPartyMembers) {
			return _nullCombatant;
		}
		return PartyMembers[index - 1];
	}

	public static VariableDictionary GetMyself() {
		UpdateState();
		return Myself ?? NullCombatant;
	}

	public static VariableDictionary GetNamedPartyMember(string name) {
		UpdateState();
		foreach (var vc in PartyMembers.Where(vc => vc.GetValue("name").ToString() == name)) return vc;
		return _nullCombatant;
	}

	public static VariableDictionary GetIdPartyMember(string id) {
		UpdateState();
		foreach (var vc in PartyMembers.Where(vc => string.Compare(vc.GetValue("id").ToString(), id, true) == 0)) return vc;
		return _nullCombatant;
	}

	#endregion Combatants
}