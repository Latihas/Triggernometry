using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dalamud.Bindings.ImGui;
using Dalamud.Utility;
using Triggernometry.Core;
using Triggernometry.Core.Conditions;
using Triggernometry.Localization;
using TriggernometryProxy;
using static Triggernometry.Core.ActionOld;

namespace Triggernometry.UI.CustomControls;

public class UserInterface {
	public enum ImageIndices {
		FolderClosed = 0,
		FolderOpen = 1,
		Bolt = 2,
		RemoteRepo = 3,
		RemoteRepoUnavailable = 4,
		LimitedFolderClosed = 5,
		LimitedFolderOpen = 6,
		Readme = 7
	}

	public static UserInterface Instance = new();
	public TreeViewEx treeView1 = new();
	// internal WMPLib.WindowsMediaPlayer wmp;
	// internal SpeechSynthesizer tts;
	// internal RealPlugin plug;
	// internal Configuration cfg;
	// internal int numLoadedTriggers;
	// internal int numLoadedFolders;
	// internal Random r = new Random();
	// internal Font _OriginalFont = null;
	// internal Font _FontOverride = null;
	//
	// internal Queue<Toast> Toasts = new Queue<Toast>();
	//
	// internal object formmgmt = new object();
	// internal Forms.LogForm formlog { get; set; } = null;
	// internal Forms.SearchForm formsearch { get; set; } = null;
	// internal string[] testInputHistoryLines = null;
	// internal string testInputHistoryZone = "";
	//
	// private Color disabledNodeColor;
	//
	// public delegate string TempDelegate(object sender);
	// private delegate void ProgressDelegate(int progress, string state);
	// internal delegate void VoidDelegate(object sender, EventArgs e);
	// internal delegate bool BoolDelegate(object sender, EventArgs e);

	public static ImageIndices GetImageIndexForClosedFolder(Folder f) => f.IsLimited() ? ImageIndices.LimitedFolderClosed : ImageIndices.FolderClosed;

	public static ImageIndices GetImageIndexForOpenFolder(Folder f) => f.IsLimited() ? ImageIndices.LimitedFolderOpen : ImageIndices.FolderOpen;

	public static ImageIndices GetImageIndexForClosedFolder(RepositoryFolder f) => f.IsLimited() ? ImageIndices.LimitedFolderClosed : ImageIndices.FolderClosed;

	public static ImageIndices GetImageIndexForOpenFolder(RepositoryFolder f) => f.IsLimited() ? ImageIndices.LimitedFolderOpen : ImageIndices.FolderOpen;

	// Create a node sorter that implements the IComparer interface.
	public class NodeSorter : IComparer {
		private bool DetermineSortOrder(TreeNode parent) {
			if (parent?.Tag is Folder folder) {
				return folder.DescendingSort;
			}
			return false;
		}

		// Compare the length of the strings, or the strings
		// themselves, if they are the same length.
		public int Compare(object x, object y) {
			var tx = x as TreeNode;
			var ty = y as TreeNode;
			if (tx.Tag is Folder && ty.Tag is Trigger) {
				return -1;
			}
			if (ty.Tag is Folder && tx.Tag is Trigger) {
				return 1;
			}
			// Both folders / triggers:
			return DetermineSortOrder(tx.Parent) ? string.Compare(ty.Text, tx.Text) : string.Compare(tx.Text, ty.Text);
		}
	}

	// public UserInterface()
	// {
	//    // Del
	// }

	// private void UserInterface_Disposed(object sender, EventArgs e)
	// {
	//    // Del       
	// }

	private bool ContainsNode(TreeNode node1, TreeNode node2) {
		if (node2.Parent == null) return false;
		if (node2.Parent.Equals(node1)) return true;
		return ContainsNode(node1, node2.Parent);
	}

	// private bool IsPartOfRemote(TreeNode tn)
	// {
	//    // Del
	// }

	// internal void TreeView1_DragOver(object sender, DragEventArgs e)
	// {
	//    // Del
	// }

	internal void TreeView1_DragEnter(object sender, DragEventArgs e) {
		e.Effect = e.AllowedEffect;
	}

	// private delegate Toast ToastDelegate();

	// private Toast PopToast()
	// {
	//    // Del
	// }
	//
	// private void HideToast(Toast t, bool result)
	//    // Del
	// }
	//
	// internal void QueueToast(Toast t)
	//    // Del
	// }

	// internal void TreeView1_DragDrop(object sender, DragEventArgs e)
	// {
	//    // Del
	// }

	// internal void TreeView1_ItemDrag(object sender, ItemDragEventArgs e)
	//    // Del
	// }
	//
	// private void SetupAutoUpdates(Toast t, bool result)
	// {
	//    // Del
	// }
	//
	// private void AddDefaultRepository()
	// {
	//    // Del
	// }
	//
	// private void SetupDefaultRepository(Toast t, bool result)
	// {
	//    // Del
	// }
	//
	// /* 移除默认逻辑
	// internal void SetupToasts()
	// {
	//    // Del
	// }*/
	//
	// // start
	// internal void SetupToasts()
	// {
	//    // Del
	// }
	// // end
	//
	// internal void ComplainAboutReload()
	// {
	//    // Del
	// }
	//
	// internal void BuildFullTreeFromConfiguration()
	// {
	//    // Del
	// }
	//
	// private void RetranslateTreeRoots()
	// {
	//    // Del
	// }
	//
	// internal void BuildTreeFromConfiguration(TreeNode parentnode, Folder parentfolder, bool parentdis)
	// {
	//    // Del
	// }
	//
	// internal void btnAddTriggerFolder_Click(object sender, EventArgs e)
	// {
	//    // Del
	// }
	//
	// internal void btnAddTrigger_Click(object sender, EventArgs e)
	// {
	//    // Del
	// }
	//
	// internal void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
	// {
	//    // Del
	// }
	//
	// internal void btnEdit_Click(object sender, EventArgs e)
	// {
	//    // Del
	// }
	//
	// internal void folderToolStripMenuItem_Click(object sender, EventArgs e)
	// {
	//    // Del
	// }
	//
	// internal void triggerToolStripMenuItem_Click(object sender, EventArgs e)
	// {
	//    // Del
	// }
	//
	// internal void removeToolStripMenuItem_Click(object sender, EventArgs e)
	// {
	//    // Del
	// }
	//
	// internal void editToolStripMenuItem_Click(object sender, EventArgs e)
	// {
	//    // Del
	// }
	//
	// internal void ctxDescendingSort_Click(object sender, EventArgs e)
	// {
	//    // Del
	// }
	//
	// internal void importToolStripMenuItem_Click(object sender, EventArgs e)
	// {
	//    // Del
	// }
	//
	// internal void exportToolStripMenuItem_Click(object sender, EventArgs e)
	// {
	//    // Del
	// }
	//
	// internal void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
	// {
	//    // Del
	// }
	//
	// internal void CountItems(Folder f, ref int numfolders, ref int numtriggers)
	// {
	//    // Del
	// }
	//
	public static void RemoveAllTriggers(Folder f) {
		foreach (var t in f.Triggers) {
			RealPlugin.Instance.RemoveTrigger(t);
		}
		foreach (var fx in f.Folders) {
			RemoveAllTriggers(fx);
		}
	}

	//
	// private delegate void RepoTreeDelegate(Repository r);
	//
	// internal void ClearRepositoryInTree(Repository r)
	// {
	//    // Del
	// }
	//
	internal static void ClearRepository(Repository r) {
		RemoveAllTriggers(r.Root);
		r.Root.Triggers.Clear();
		r.Root.Folders.Clear();
		// ClearRepositoryInTree(r);
	}

	//
	// internal void CopySelected()
	// {
	//    // Del
	// }
	//
	// internal void btnRemoveTrigger_Click(object sender, EventArgs e)
	// {
	//    // Del
	// }
	//
	// internal void ImportResultsFromForm(Forms.ImportForm imf)
	// {
	//    // Del
	// }
	//
	// internal List<Folder> ConstructFolderList(List<Folder> cur, Folder f)
	// {
	//    // Del
	// }
	//
	// internal void ResetBackgrounds(TreeNode tn)
	// {
	//    // Del
	// }
	//
	// internal void AddAllTriggersFromFolder(Folder f, ref List<Trigger> trigs)
	// {
	//    // Del
	// }
	//
	// internal TriggernometryExport ExportSelection()
	// {
	//    // Del
	// }
	//
	// internal void btnExportTrigger_Click(object sender, EventArgs e)
	// {
	//    // Del
	// }
	//
	// internal void clearActionQueueToolStripMenuItem_Click(object sender, EventArgs e)
	// {
	//    // Del
	// }
	//
	// internal void enabledToolStripMenuItem_Click(object sender, EventArgs e)
	// {
	//    // Del
	// }
	//
	// internal void disabledToolStripMenuItem_Click(object sender, EventArgs e)
	// {
	//    // Del
	// }
	//
	// private void CloseTree(TreeNode tn)
	// {
	//    // Del
	// }
	//
	// internal void UpdateUiFont()
	// {
	//    // Del
	// }
	//
	// internal void configurationToolStripMenuItem_Click(object sender, EventArgs e)
	// {
	//    // Del
	// }
	//
	// internal void treeView1_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
	// {
	//    // Del
	// }
	//
	// internal void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
	// {
	//    // Del
	// }
	//
	// internal void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
	// {            
	//    // Del
	// }
	//
	// internal void aboutToolStripMenuItem_Click(object sender, EventArgs e)
	// {
	//    // Del
	// }
	//
	// internal void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
	// {
	//    // Del
	// }
	//
	// internal void OpenLogForm(bool errorSearch)
	// {
	//    // Del
	// }
	//
	// internal void OpenSearchForm()
	// {
	//    // Del
	// }
	//
	// internal void viewLogToolStripMenuItem_Click(object sender, EventArgs e)
	// {
	//    // Del
	// }
	//
	// private void Formlog_FormClosed(object sender, FormClosedEventArgs e)
	// {
	//    // Del
	// }
	//
	// private void Formsearch_FormClosed(object sender, FormClosedEventArgs e)
	// {
	//    // Del
	// }
	//
	// private void killExecutingActionsToolStripMenuItem_Click(object sender, EventArgs e)
	// {
	//    // Del
	// }
	//
	// private void viewVariablesToolStripMenuItem_Click(object sender, EventArgs e)
	// {
	//    // Del
	// }
	//
	// private void RecolorStartingFromNode(TreeNode tn, bool pstate, bool refreshActives)
	// {
	//    // Del
	// }
	//
	// private Folder GetTopLevelFolder(Trigger t)
	// {
	//    // Del
	// }
	//
	// private Folder GetTopLevelFolder(Folder f)
	// {
	//    // Del
	// }
	//
	// private void treeView1_AfterCheck(object sender, TreeViewEventArgs e)
	// {
	//    // Del
	// }
	//
	// private void deactivateAllAurasToolStripMenuItem_Click(object sender, EventArgs e)
	// {
	//    // Del
	// }
	//
	// private void testInputToolStripMenuItem_Click(object sender, EventArgs e)
	// {
	//    // Del
	// }
	//
	// internal void CloseForms()
	// {
	//    // Del
	// }
	//
	// private void rfunBenchmarkToolStripMenuItem_Click(object sender, EventArgs e)
	// {
	//    // Del
	// }
	//
	// private int _newErrorCount = 0;
	// /// <summary> Should only use this property on the main UI thread. </summary>
	// private int NewErrorCount {
	//    // Del
	// }
	//
	// private void UpdateErrorCount()
	// {
	//    // Del
	// }
	//
	// internal void ClearErrorCount()
	// {
	//     if (this.IsDisposed) return;
	//     if (this.InvokeRequired)
	//     {
	//         this.Invoke(new System.Action(ClearErrorCount));
	//     }
	//     else
	//     {
	//         NewErrorCount = 0;
	//     }
	// }
	//
	// internal void IncrementErrorCount()
	// {
	//     if (this.IsDisposed) return;
	//     if (this.InvokeRequired)
	//     {
	//         this.Invoke(new System.Action(IncrementErrorCount));
	//     }
	//     else
	//     {
	//         NewErrorCount += 1;
	//     }
	// }
	//
	// internal void PasteSelected()
	// {
	//     string data = null;
	//     try
	//     {                
	//         data = System.Windows.Forms.Clipboard.GetText(TextDataFormat.UnicodeText);
	//         if (data != null && data.Length > 0)
	//         {
	//             TriggernometryExport exp = TriggernometryExport.Unserialize(data);
	//             using (Forms.ImportForm ifo = new Forms.ImportForm(plug))
	//             {
	//                 ifo.BuildTreeFromExport(exp, null, null, false);
	//                 ImportResultsFromForm(ifo);
	//             }
	//         }
	//     }
	//     catch (Exception ex)
	//     {
	//         plug.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/UserInterface/pastefail", "Tree paste failed due to exception: {0}", ex.Message));
	//     }
	// }
	//
	// private void treeView1_KeyDown(object sender, KeyEventArgs e)
	// {
	//     if (e.KeyCode == Keys.C && e.Modifiers == Keys.Control)
	//     {
	//         CopySelected();
	//     }
	//     else if (e.KeyCode == Keys.V && e.Modifiers == Keys.Control)
	//     {
	//         if (btnAdd.Enabled == true && btnAddTrigger.Enabled == true)
	//         {
	//             PasteSelected();
	//         }
	//     }
	//     else if (e.KeyCode == Keys.Delete)
	//     {
	//         if (btnRemoveTrigger.Enabled == true)
	//         {
	//             btnRemoveTrigger_Click(this, null);
	//         }
	//     }
	// }
	//
	// private void copyToolStripMenuItem_Click(object sender, EventArgs e)
	// {
	//     CopySelected();
	// }
	//
	// private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
	// {
	//     if (btnAdd.Enabled == true && btnAddTrigger.Enabled == true)
	//     {
	//         PasteSelected();
	//     }
	// }
	//
	// internal void ClearAllVariables()
	// {
	//     var result = MessageBox.Show(I18n.Translate("internal/UserInterface/clearvar", "Are you sure you want to clear the session variables?"), "", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
	//
	//     if (result != DialogResult.Yes) return;
	//
	//     lock (plug.sessionvars.Scalar)
	//     {
	//         plug.sessionvars.Scalar.Clear();
	//     }
	//     lock (plug.sessionvars.List)
	//     {
	//         plug.sessionvars.List.Clear();
	//     }
	//     lock (plug.sessionvars.Table)
	//     {
	//         plug.sessionvars.Table.Clear();
	//     }
	//     lock (plug.sessionvars.Dict)
	//     {
	//         plug.sessionvars.Dict.Clear();
	//     }
	// }
	//
	// private void clearAllVariablesToolStripMenuItem_Click(object sender, EventArgs e)
	// {
	//     ClearAllVariables();
	// }
	//
	// private void saveConfigurationManuallyToolStripMenuItem_Click(object sender, EventArgs e)
	// {
	//     if (plug.configBroken == false)
	//     {
	//         plug.SaveCurrentConfig();
	//     }
	//     else
	//     {
	//         string exx = I18n.Translate("internal/UserInterface/warnbeforemanualsave", "Errors were encountered when loading configuration on startup. Saving configuration in this state may lead to it being corrupted beyond repair. Are you sure you want to save configuration anyway?");
	//         if (MessageBox.Show(this, exx, I18n.Translate("internal/UserInterface/saveconfigmanually", "Save configuration manually"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
	//         {
	//             plug.SaveCurrentConfig();
	//             plug.configBroken = false;
	//         }
	//     }
	// }
	//
	// private void btnCloseWelcome_Click(object sender, EventArgs e)
	// {
	//     cfg.ShowWelcome = chkWelcome.Checked;
	//     btnOptions.Enabled = true;
	//     pnlUi.Visible = true;
	//     pnlWelcome.Visible = false;
	//     toolStrip1.SendToBack();
	//     treeView1.SelectedNode = treeView1.Nodes[0];
	//     treeView1.Focus();
	// }
	//
	// private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
	// {
	//     System.Diagnostics.Process.Start(linkLabel1.Text);
	// }
	//
	// private void goToConfigurationFolderToolStripMenuItem_Click(object sender, EventArgs e)
	// {
	//     string pth = plug.path;
	//     Process.Start(pth);
	// }
	//
	// private void remoteTriggerRepositoryToolStripMenuItem_Click(object sender, EventArgs e)
	// {
	//     using (Forms.RepositoryForm rf = new Forms.RepositoryForm())
	//     {
	//         rf.plug = plug;
	//         rf.SettingsFromRepository(null);
	//         rf.Text = I18n.Translate("internal/UserInterface/addrepo", "Add new repository");
	//         rf.btnOk.Text = I18n.Translate("internal/UserInterface/add", "Add");
	//         if (rf.ShowDialog() == DialogResult.OK)
	//         {
	//             Repository r = new Repository();
	//             r.Enabled = true;
	//             rf.SettingsToRepository(r);
	//             TreeNode tn = new TreeNode();
	//             tn.Text = r.Name;
	//             tn.Tag = r;
	//             tn.Checked = r.Enabled;
	//             tn.ImageIndex = (int)ImageIndices.RemoteRepoUnavailable;
	//             tn.SelectedImageIndex = tn.ImageIndex;
	//             RepositoryFolder rfo = (RepositoryFolder)treeView1.SelectedNode.Tag;
	//             rfo.Repositories.Add(r);
	//             r.Parent = rfo;
	//             treeView1.SelectedNode.Nodes.Add(tn);
	//             treeView1.SelectedNode.Expand();
	//             treeView1.Sort();
	//             treeView1.SelectedNode = tn;
	//             RecolorStartingFromNode(tn.Parent, tn.Parent.Checked, true);
	//             btnUpdate_Click(sender, e);
	//         }
	//     }
	// }
	//
	// private void repositoryToolStripMenuItem_Click(object sender, EventArgs e)
	// {
	//     remoteTriggerRepositoryToolStripMenuItem_Click(sender, e);
	// }
	//
	// internal void SetupLanguageMenu()
	// {
	//     if (I18n.RegisteredLanguages.Count > 0)
	//     {
	//         foreach (KeyValuePair<string, Language> kp in I18n.RegisteredLanguages)
	//         {
	//             if (kp.Value.IsDefault == true)
	//             {
	//                 continue;
	//             }
	//             if (toolStripSeparator10.Visible == false)
	//             {
	//                 toolStripSeparator10.Visible = true;
	//             }
	//             ToolStripMenuItem tsi = new ToolStripMenuItem();
	//             tsi.Text = kp.Value.LanguageName;
	//             if (I18n.CurrentLanguage != null)
	//             {
	//                 if (I18n.CurrentLanguage.LanguageName == tsi.Text)
	//                 {
	//                     if (btnDefaultLanguage.Checked == true)
	//                     {
	//                         btnDefaultLanguage.Checked = false;
	//                         btnDefaultLanguage.Enabled = true;
	//                     }
	//                     tsi.Checked = true;
	//                     tsi.Enabled = false;
	//                 }
	//             }
	//             tsi.CheckOnClick = true;
	//             tsi.CheckedChanged += Tsi_CheckedChanged;
	//             btnLanguages.DropDownItems.Add(tsi);
	//         }
	//     }
	//     btnDefaultLanguage.CheckedChanged += Tsi_CheckedChanged;
	//     foreach (ToolStripItem tsi in btnLanguages.DropDownItems)
	//     {
	//         tsi.Tag = I18n.DoNotTranslate;
	//     }
	// }
	//
	// private void Tsi_CheckedChanged(object sender, EventArgs e)
	// {
	//     ToolStripMenuItem tsi = (ToolStripMenuItem)sender;
	//     if (tsi.Checked == true)
	//     {
	//         tsi.Enabled = false;
	//         foreach (ToolStripItem tsim in btnLanguages.DropDownItems)
	//         {
	//             if (tsim == tsi)
	//             {
	//                 continue;
	//             }
	//             if (tsim is ToolStripMenuItem)
	//             {
	//                 ToolStripMenuItem tsimm = (ToolStripMenuItem)tsim;
	//                 tsimm.Checked = false;
	//             }
	//         }
	//         if (tsi == btnDefaultLanguage)
	//         {
	//             plug.ChangeLanguage(null);
	//             RetranslateTreeRoots();
	//             I18n.TranslateControl("Plugin", this);
	//         }
	//         else
	//         {
	//             plug.ChangeLanguage(sender.ToString());
	//             RetranslateTreeRoots();
	//             I18n.TranslateControl("Plugin", this);
	//         }
	//     }
	//     else
	//     {
	//         tsi.Enabled = true;
	//     }
	// }
	//
	// internal void ShowProgress(int progress, string state)
	// {
	//     if (statusStrip1.InvokeRequired == true)
	//     {
	//         statusStrip1.Invoke(new ProgressDelegate(ShowProgress), progress, state);
	//         return;
	//     }
	//     if (progress == 0 && state == "" && statusStrip1.Visible == true)
	//     {
	//         statusStrip1.Visible = false;
	//         return;
	//     }
	//     if (progress == -1)
	//     {
	//         prgStatus.Style = ProgressBarStyle.Marquee;
	//     }
	//     else
	//     {
	//         prgStatus.Style = ProgressBarStyle.Continuous;
	//         if (progress < prgStatus.Minimum)
	//         {
	//             progress = prgStatus.Minimum;
	//         }
	//         if (progress > prgStatus.Maximum)
	//         {
	//             progress = prgStatus.Maximum;
	//         }
	//         prgStatus.Value = progress;
	//     }
	//     tlsStatus.Text = state;
	//     if (progress != 0 && statusStrip1.Visible == false)
	//     {
	//         statusStrip1.Visible = true;
	//     }
	// }
	//
	// private delegate void RepoTreeBuilderDelegate(TriggernometryExport exp, Repository r);
	//
	// internal void BuildTreeForRepository(TriggernometryExport exp, Repository r)
	// {
	//     if (treeView1.InvokeRequired == true)
	//     {
	//         treeView1.Invoke(new RepoTreeBuilderDelegate(BuildTreeForRepository), exp, r);
	//         return;
	//     }
	//     using (Forms.ImportForm ifo = new Forms.ImportForm(plug))
	//     {
	//         foreach (TreeNode tn in treeView1.Nodes[1].Nodes)
	//         {
	//             if (tn.Tag == r)
	//             {
	//                 ifo.BuildTreeFromExport(exp, tn, r.Root, true);
	//                 tn.ImageIndex = (int)ImageIndices.RemoteRepo;
	//                 tn.SelectedImageIndex = tn.ImageIndex;
	//                 if (r.ReadmeTriggers.Count > 0)
	//                 {
	//                     foreach (Trigger t in r.ReadmeTriggers)
	//                     {
	//                         TreeNode tx = plug.LocateNodeHostingTrigger(tn, t);
	//                         if (tx != null)
	//                         {
	//                             TreeNode rn = new TreeNode();
	//                             rn.Text = tx.Text;
	//                             rn.Checked = tx.Checked;
	//                             rn.Tag = tx.Tag;
	//                             rn.ImageIndex = (int)ImageIndices.Readme;
	//                             rn.SelectedImageIndex = (int)ImageIndices.Readme;                                    
	//                             tn.Nodes.Add(rn);
	//                             rn.EnsureVisible();
	//                         }
	//                     }
	//                 }
	//                 RecolorStartingFromNode(tn, r.Enabled, false);
	//                 return;
	//             }
	//         }
	//     }
	// }
	//

	public static void ForceUpdateRepository(object tnupdate) {
		if (tnupdate == null) {
			return;
		}
		if (tnupdate is RepositoryFolder folder) {
			// foreach (TreeNode tn in tnupdate.Nodes)
			// {
			//     tn.ImageIndex = (int)ImageIndices.RemoteRepoUnavailable;
			//     tn.SelectedImageIndex = tn.ImageIndex;
			// }
			_ = RealPlugin.Instance.UpdateAllRepositoriesAsync(false);
		}
		if (tnupdate is Repository rfo) {
			// tnupdate.ImageIndex = (int)ImageIndices.RemoteRepoUnavailable;
			// tnupdate.SelectedImageIndex = tnupdate.ImageIndex;
			_ = RealPlugin.Instance.UpdateRepositoriesAsync([rfo], false);
		}
	}

	private static void ForceFireTrigger(Trigger t, TriggerForceTypeEnum force) {
		var ctx = new Context(t) {
			testByPlaceholder = false,
			soundhook = RealPlugin.Instance.SoundPlaybackSmart,
			ttshook = RealPlugin.Instance.TtsPlaybackSmart,
			triggeredTime = DateTime.UtcNow,
			forceType = force
		};
		if ((t.TestInput?.Length ?? 0) == 0)
			t.Fire(ctx);
		else {
			var lines = ctx.EvaluateStringExpression(t.TriggerContextLogger, RealPlugin.Instance, t.TestInput).Split([
				"\r\n", "\r", "\n"
			], StringSplitOptions.None);
			force = TriggerForceTypeEnum.SkipActive | TriggerForceTypeEnum.SkipRefire | TriggerForceTypeEnum.SkipParent;
			LogEvent.SourceEnum source;
			switch (t.Source) {
				case Trigger.TriggerSourceEnum.ACT:
					source = LogEvent.SourceEnum.ACT;
					break;
				case Trigger.TriggerSourceEnum.Endpoint:
					source = LogEvent.SourceEnum.Endpoint;
					break;
				case Trigger.TriggerSourceEnum.FFXIVNetwork:
					source = LogEvent.SourceEnum.NetworkFFXIV;
					break;
				case Trigger.TriggerSourceEnum.Log:
					source = LogEvent.SourceEnum.Log;
					break;
				default:
				case Trigger.TriggerSourceEnum.None:
					force |= TriggerForceTypeEnum.SkipRegexp;
					source = LogEvent.SourceEnum.Log;
					break;
			}
			foreach (var line in lines) {
				var le = new LogEvent {
					Text = line,
					Timestamp = DateTime.Now,
					TestMode = true,
					ZoneName = "",
					ZoneId = null,
					Source = source
				};
				RealPlugin.Instance.TestTrigger(t, le, force);
			}
		}
	}

	internal static TriggernometryExport ExportSelection(object Tag) {
		var exp = new TriggernometryExport {
			PluginVersion = RealPlugin.Instance.cfg.PluginVersion
		};
		switch (Tag) {
			case Trigger trigger:
				exp.ExportedTrigger = trigger;
				break;
			case Folder tag:
				exp.ExportedFolder = tag;
				break;
			case RepositoryFolder folder:
				exp.ExportedFolder = folder.ConvertToFolder();
				break;
			case Repository repository:
				exp.ExportedFolder = repository.Root;
				break;
		}
		return exp;
	}

	//
	// private void btnUpdate_Click(object sender, EventArgs e)
	// {
	//     ForceUpdateRepository(treeView1.SelectedNode);
	// }
	//
	// private void ctxUpdate_Click(object sender, EventArgs e)
	// {
	//     btnUpdate_Click(sender, e);
	// }
	//
	// private void btnAddRepoList_Click(object sender, EventArgs e)
	// {
	//     using (Forms.RepositoryListForm rlf = new Forms.RepositoryListForm())
	//     {
	//         rlf.plug = plug;
	//         rlf.Text = I18n.Translate("internal/UserInterface/addrepolist", "Add new repository from list");
	//         if (rlf.ShowDialog() == DialogResult.OK)
	//         {
	//             foreach (RepositoryList.Repository rta in rlf.ReposToAdd)
	//             {
	//                 Repository r = new Repository();
	//                 r.Enabled = true;
	//                 r.Address = rta.Address;
	//                 r.AllowProcessLaunch = false;
	//                 r.AllowWindowMessages = false;
	//                 r.AllowScriptExecution = false;
	//                 r.AllowObsControl = false;
	//                 r.AllowDiskOperations = false;
	//                 r.KeepLocalBackup = true;
	//                 r.Name = rta.Name;
	//                 r.NewBehavior = Repository.NewBehaviorEnum.AsDefined;
	//                 r.UpdatePolicy = Repository.UpdatePolicyEnum.Startup;
	//                 r.AudioOutput = Repository.AudioOutputEnum.NeverOverride;
	//                 TreeNode tn = new TreeNode();
	//                 tn.Text = r.Name;
	//                 tn.Tag = r;
	//                 tn.Checked = r.Enabled;
	//                 tn.ImageIndex = (int)ImageIndices.RemoteRepoUnavailable;
	//                 tn.SelectedImageIndex = tn.ImageIndex;
	//                 RepositoryFolder rfo = (RepositoryFolder)treeView1.Nodes[1].Tag;
	//                 rfo.Repositories.Add(r);
	//                 r.Parent = rfo;
	//                 treeView1.Nodes[1].Nodes.Add(tn);
	//                 treeView1.Nodes[1].Expand();
	//                 RecolorStartingFromNode(tn.Parent, tn.Parent.Checked, true);
	//                 treeView1.Sort();
	//                 ForceUpdateRepository(tn);
	//             }
	//         }
	//     }
	// }
	//
	// private void ctxAddRepoList_Click(object sender, EventArgs e)
	// {
	//     btnAddRepoList_Click(sender, e);
	// }
	//
	// private void errThing1_Click(object sender, EventArgs e)
	// {
	//     OpenLogForm(true);
	//     NewErrorCount = 0;
	// }
	//
	// private void ctxCollapse_Click(object sender, EventArgs e)
	// {
	//     if (treeView1.SelectedNode != null)
	//     {
	//         treeView1.SelectedNode.Collapse(false);
	//     }
	//     else
	//     {
	//         treeView1.CollapseAll();
	//     }
	// }
	//
	// private void ctxExpand_Click(object sender, EventArgs e)
	// {
	//     if (treeView1.SelectedNode != null)
	//     {
	//         treeView1.SelectedNode.ExpandAll();
	//     }
	//     else
	//     {
	//         treeView1.ExpandAll();
	//     }
	// }
	//
	// private static readonly List<string> _legalRepoPrefixes = new List<string> {
	//     "https://github.com/paissaheavyindustries/Triggernometry",
	//     "https://vip.123pan.cn/1824544011/"
	// };
	//
	public static void AddRepo(Repository r, bool shouldUpdate) {
		// if (InvokeRequired)
		// {
		//     Invoke(new System.Action(() => AddRepo(r, shouldUpdate)));
		//     return;
		// }
		// if (!_legalRepoPrefixes.Any(prefix => r.Address.StartsWith(prefix)))
		// {
		//     plug.UnfilteredAddToLog(RealPlugin.DebugLevelEnum.Error,
		//         I18n.IsChineseEnvironment
		//         ? $"正在尝试添加的远程仓库地址 {r.Address} 未在信任列表内，你需要手动添加此远程仓库。"
		//         : $"The repository address {r.Address} you are trying to add is not a trusted address and needs to be added manually."
		//     );
		//     return;
		// }

		var rfo = RealPlugin.Instance.cfg.RepositoryRoot;
		var tn = rfo.Repositories
			.Where(repo => repo.Address == r.Address)
			// .Select(repo => treeView1.Nodes[1].Nodes.Cast<TreeNode>().FirstOrDefault(node => node.Tag == repo))
			.FirstOrDefault();
		if (tn != null) {
			tn.Name = r.Name;
			// existingRepo.Enabled = r.Enabled;
			tn.AllowProcessLaunch = r.AllowProcessLaunch;
			tn.AllowScriptExecution = r.AllowScriptExecution;
			tn.AllowDiskOperations = r.AllowDiskOperations;
			tn.AllowWindowMessages = r.AllowWindowMessages;
			tn.AllowObsControl = r.AllowObsControl;
			tn.KeepLocalBackup = r.KeepLocalBackup;
			tn.UpdatePolicy = r.UpdatePolicy;
			tn.AudioOutput = r.AudioOutput;

			// tn.Text = existingRepo.Name;
			// tn.Checked = existingRepo.Enabled;
			// tn.ImageIndex = (int)ImageIndices.RemoteRepoUnavailable;
			// tn.SelectedImageIndex = tn.ImageIndex;
		} else {
			tn = new Repository {
				// Text = r.Name,
				// Tag = r,
				// Checked = r.Enabled,
				// ImageIndex = (int)ImageIndices.RemoteRepoUnavailable
			};
			// tn.SelectedImageIndex = tn.ImageIndex;
			rfo.Repositories.Add(r);
			r.Parent = rfo;
			// RealPlugin.plug. cfg.RepositoryRoot.Repositories. Add(tn);
			// treeView1.Nodes[1].Expand();
		}
		// RecolorStartingFromNode(tn.Parent, tn.Parent.Checked, true);
		// treeView1.Sort();
		if (shouldUpdate) {
			ForceUpdateRepository(tn);
		}
	}

	public static void AddRepos(IEnumerable<Repository> repos, bool shouldUpdate) {
		foreach (var r in repos) {
			AddRepo(r, shouldUpdate);
		}
	}

	public static void RemoveRepo(string partialUrl) {
		// if (InvokeRequired)
		// {
		//     Invoke(new System.Action(() => RemoveRepo(partialUrl)));
		//     return;
		// }
		partialUrl = partialUrl.Trim().ToLower();
		var rfo = RealPlugin.Instance.cfg.RepositoryRoot;
		var nodes = rfo.Repositories
			.Where(repo => repo.Address.IsNullOrEmpty() || repo.Address.ToLower().Contains(partialUrl))
			// .Select(repo => treeView1.Nodes[1].Nodes.Cast<TreeNode>().FirstOrDefault(node => node.Tag == repo))
			.ToList();

		for (var i = 0; i < nodes.Count; i++) {
			var tn = nodes[i];
			if (tn == null) continue;
			tn.Enabled = false;
			rfo.Repositories.Remove(tn);
			RealPlugin.Instance.cfg.RepositoryRoot.Repositories.Remove(tn);
		}
	}

	public static Repository DefaultRepoCN(string address, string name, int updateIntervalMinutes) =>
		new() {
			Enabled = true,
			Address = address,
			AllowProcessLaunch = true,
			AllowScriptExecution = true,
			KeepLocalBackup = true,
			Name = name,
			NewBehavior = Repository.NewBehaviorEnum.AsDefined,
			UpdatePolicy = Repository.UpdatePolicyEnum.Startup,
			AudioOutput = Repository.AudioOutputEnum.NeverOverride,
			AutoUpdate = true,
			UpdateInterval = updateIntervalMinutes
		};

	public static void BuildTriggerTreeFromConfiguration(object? parentTag, Folder? parentfolder) {
		if (parentTag == null || parentfolder == null) {
			lock (RealPlugin.Instance.Triggers) RealPlugin.Instance.Triggers.Clear();
			lock (RealPlugin.Instance.ActiveTextTriggers) RealPlugin.Instance.ActiveTextTriggers.Clear();
			lock (RealPlugin.Instance.ActiveFFXIVNetworkTriggers) RealPlugin.Instance.ActiveFFXIVNetworkTriggers.Clear();
			lock (RealPlugin.Instance.ActiveACTTriggers) RealPlugin.Instance.ActiveACTTriggers.Clear();
			lock (RealPlugin.Instance.ActiveEndpointTriggers) RealPlugin.Instance.ActiveEndpointTriggers.Clear();
			lock (RealPlugin.Instance.ActionQueue) RealPlugin.Instance.ActionQueue.Clear();
			BuildTriggerTreeFromConfiguration(RealPlugin.Instance.cfg.Root, RealPlugin.Instance.cfg.Root);
			foreach (var r in RealPlugin.Instance.cfg.RepositoryRoot.Repositories) {
				r.Parent = RealPlugin.Instance.cfg.RepositoryRoot;
				BuildTriggerTreeFromConfiguration(r.Root, r.Root);
			}
			return;
		}
		var sortedFolders = parentfolder.Folders.OrderBy(fx => fx.Name);
		foreach (var fx in sortedFolders) {
			fx.Parent = parentfolder;
			BuildTriggerTreeFromConfiguration(fx, fx);
		}
		var sortedTriggers = parentfolder.Triggers.OrderBy(tx => tx.Name);
		foreach (var tx in sortedTriggers) {
			tx.Parent = parentfolder;
			RealPlugin.Instance.AddTrigger(tx, tx.Parent.ParentsEnabled());
			if (tx.Condition != null) ConditionGroup.RebuildParentage(tx.Condition);
			foreach (var a in tx.Actions)
				if (a.Condition != null)
					ConditionGroup.RebuildParentage(a.Condition);
		}
	}

	public static void BuildRenderTreeFromConfiguration(object? parentTag, Folder? parentfolder, bool parentDisabled) {
		try {
			if (parentTag == null || parentfolder == null) {
				RenderTreeNode(
					"本地触发器",
					RealPlugin.Instance.cfg.Root,
					parentDisabled,
					() => BuildRenderTreeFromConfiguration(RealPlugin.Instance.cfg.Root, RealPlugin.Instance.cfg.Root, !RealPlugin.Instance.cfg.Root.Enabled));
				RenderTreeNode(
					I18n.Translate("internal/UserInterface/remote", "Remote triggers"),
					RealPlugin.Instance.cfg.RepositoryRoot,
					parentDisabled,
					() => {
						foreach (var r in RealPlugin.Instance.cfg.RepositoryRoot.Repositories) {
							r.Parent = RealPlugin.Instance.cfg.RepositoryRoot;
							var isRepoDisabled = !RealPlugin.Instance.cfg.RepositoryRoot.Enabled || !r.Enabled;
							RenderTreeNode(
								r.Name,
								r,
								isRepoDisabled,
								() => BuildRenderTreeFromConfiguration(r.Root, r.Root, !RealPlugin.Instance.cfg.RepositoryRoot.Enabled)
							);
						}
					}
				);
				return;
			}
			var sortedFolders = parentfolder.Folders.OrderBy(fx => fx.Name);
			foreach (var fx in sortedFolders) {
				fx.Parent = parentfolder;
				var isFolderDisabled = parentDisabled || !fx.Enabled;
				RenderTreeNode(
					fx.Name,
					fx,
					isFolderDisabled,
					() => BuildRenderTreeFromConfiguration(fx, fx, isFolderDisabled)
				);
			}
			var sortedTriggers = parentfolder.Triggers.OrderBy(tx => tx.Name);
			foreach (var tx in sortedTriggers) {
				tx.Parent = parentfolder;
				var isTriggerDisabled = parentDisabled || !tx.Enabled;
				RenderTreeNode(
					tx.Name,
					tx,
					isTriggerDisabled,
					null
				);
			}
		} catch (Exception) {
		}
	}

	private static readonly Dictionary<object, bool> _nodeExpandedStates = new();
	private static readonly Dictionary<object, bool> _nodeCheckedStates = new();
	private static readonly Vector4 ColorGrey = new(0.5f, 0.5f, 0.5f, 1.0f);

	public static void ImportResultsFromForm(object tag, string content) {
		try {
			var ser = TriggernometryExport.Unserialize(content);
			if (ser == null || ser.ExportedFolder == null && ser.ExportedTrigger == null)
				return;
			Folder df;
			switch (tag) {
				case Folder tagFolder:
					df = tagFolder;
					break;
				case Trigger tagTrigger:
					df = tagTrigger.Parent;
					break;
				default:
					return;
			}
			Dictionary<Guid, Guid> renamedFolders = new();
			if (ser.ExportedFolder != null) {
				var importedFolders = ConstructFolderList(null, ser.ExportedFolder);
				var existingFolders = ConstructFolderList(null, RealPlugin.Instance.cfg.Root);
				foreach (var importedFolder in importedFolders)
					if (existingFolders.Any(existing => existing.Id == importedFolder.Id)) {
						var oldId = importedFolder.Id;
						importedFolder.Id = Guid.NewGuid();
						renamedFolders[oldId] = importedFolder.Id;
						RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Warning,
							I18n.Translate("internal/UserInterface/folderidreassign",
								"Reassigning new id ({0}) for folder ({1}) due to already assigned id ({2})",
								importedFolder.Id, importedFolder.Name, oldId));
					}
			}
			List<Trigger> importedTriggers = [];
			if (ser.ExportedFolder != null)
				AddAllTriggersFromFolder(ser.ExportedFolder, ref importedTriggers);
			else if (ser.ExportedTrigger != null)
				importedTriggers.Add(ser.ExportedTrigger);
			Dictionary<Guid, Guid> renamedTriggers = new();
			lock (RealPlugin.Instance.Triggers) {
				foreach (var importedTrigger in importedTriggers) {
					if (RealPlugin.Instance.Triggers.Any(existing => existing.Id == importedTrigger.Id && existing.Repo == null)) {
						var oldId = importedTrigger.Id;
						importedTrigger.Id = Guid.NewGuid();
						renamedTriggers[oldId] = importedTrigger.Id;
						RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Warning,
							I18n.Translate("internal/UserInterface/idreassign",
								"Reassigning new id ({0}) for trigger ({1}) due to already assigned id ({2})",
								importedTrigger.Id, importedTrigger.Name, oldId));
					}
                df.Folders.Add(f);
                f.Parent = df;
                plug.RegisterFolderVariables(f);
				}
			}
			var updatedActionTriggerRefs = 0;
			var updatedActionFolderRefs = 0;
			foreach (var trigger in importedTriggers) {
				foreach (var action in trigger.Actions) {
					if (renamedTriggers.TryGetValue(action._TriggerId, out var value)) {
						action._TriggerId = value;
						updatedActionTriggerRefs++;
					}
					if (renamedFolders.TryGetValue(action._FolderId, out var folder)) {
						action._FolderId = folder;
						updatedActionFolderRefs++;
					}
				}
			}
			if (updatedActionTriggerRefs > 0 || updatedActionFolderRefs > 0) {
				RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Warning,
					I18n.Translate("internal/UserInterface/idreassigncount",
						"Adjusted {0} trigger and {1} folder references on actions due to collisions",
						updatedActionTriggerRefs, updatedActionFolderRefs));
			}
			if (ser.ExportedFolder != null) {
				ser.ExportedFolder.Parent = df;
				df.Folders.Add(ser.ExportedFolder);
				_nodeExpandedStates[ser.ExportedFolder] = true;
			}
            int fdjs = 0, tdjs = 0;
            foreach (Trigger nt in newTrigs)
            {
                foreach (Core.ActionOld a inCore.ActionOld.RecursiveGetActions( nt.Actions))))
                {
	                if (renamedTriggers.ContainsKey(a._TriggerId) == true) {
		                a._TriggerId = renamedTriggers[a._TriggerId];
		                tdjs++;
	                }
	                if (renamedFolders.ContainsKey(a._FolderId) == true) {
		                a._FolderId = renamedFolders[a._FolderId];
		                fdjs++;
	                }
                }
                if (ser.ExportedTrigger != null) {
				ser.ExportedTrigger.Parent = df;
				df.Triggers.Add(ser.ExportedTrigger);
				_nodeExpandedStates[ser.ExportedTrigger] = true;
			}
			foreach (var trigger in importedTriggers) {
				RealPlugin.Instance.AddTrigger(trigger, trigger.Parent == null ? true : trigger.Parent.ParentsEnabled());
				if (trigger.Condition != null)
					ConditionGroup.RebuildParentage(trigger.Condition);
				foreach (var action in trigger.Actions)
					if (action.Condition != null)
						ConditionGroup.RebuildParentage(action.Condition);
			}
		} catch (Exception ex) {
			RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error, ex.ToString());
                newTrig.SetActionsParent();
                plug.AddTrigger(newTrig, newTrig.Parent.ParentsEnabled());
            }
            if (fdjs > 0 || tdjs > 0)
            {
                plug.FilteredAddToLog(RealPlugin.DebugLevelEnum.Warning, I18n.Translate("internal/UserInterface/idreassigncount", "Adjusted {0} trigger and {1} folder references on actions due to collisions", tdjs, fdjs));
		}
	}

	private static List<Folder> ConstructFolderList(List<Folder> cur, Folder f) {
		var isRootCall = false;
		if (cur == null) {
			cur = [];
			isRootCall = true;
		}
		cur.Add(f);
		foreach (var subFolder in f.Folders) ConstructFolderList(cur, subFolder);
		return isRootCall ? cur : null;
	}

	private static void AddAllTriggersFromFolder(Folder f, ref List<Trigger> trigs) {
		foreach (var subFolder in f.Folders) AddAllTriggersFromFolder(subFolder, ref trigs);
		trigs.AddRange(f.Triggers);
	}

	private static void RenderTreeNode(string text, object tag, bool isDisabled, Action? renderChildren) {
		try {
			_nodeExpandedStates.TryAdd(tag, false);
			_nodeCheckedStates[tag] = tag switch {
				Folder folder => folder.Enabled,
				Trigger trigger => trigger.Enabled,
				Repository repo => repo.Enabled,
				RepositoryFolder repoFolder => repoFolder.Enabled,
				_ => false
			};
			if (isDisabled) ImGui.PushStyleColor(ImGuiCol.Text, ColorGrey);
			var flags = ImGuiTreeNodeFlags.None;
			if (renderChildren == null) flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
			var isChecked = _nodeCheckedStates[tag];
			if (ImGui.Checkbox($"##check_{tag.GetHashCode()}", ref isChecked)) {
				switch (tag) {
					case Folder folder:
						folder.Enabled = isChecked;
						break;
					case Trigger trigger:
						trigger.Enabled = isChecked;
						if (isChecked) RealPlugin.Instance.TriggerEnabled(trigger);
						else RealPlugin.Instance.TriggerDisabled(trigger);
						break;
					case Repository repo:
						repo.Enabled = isChecked;
						break;
					case RepositoryFolder repoFolder:
						repoFolder.Enabled = isChecked;
						break;
				}
				BuildTriggerTreeFromConfiguration(null, null);
				Task.Run(RealPlugin.Instance.SaveCurrentConfig);
			}
			ImGui.SameLine();
			var isExpanded = ImGui.TreeNodeEx(
				$"{text}##{tag.GetHashCode()}",
				flags
			);
			if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) ImGui.OpenPopup($"context_menu_{tag.GetHashCode()}");
			if (ImGui.BeginPopup($"context_menu_{tag.GetHashCode()}")) {
				var tempStylePopped = false;
				if (isDisabled) {
					ImGui.PopStyleColor();
					tempStylePopped = true;
				}
				switch (tag) {
					case Repository repo: {
						if (ImGui.MenuItem("更新"))
							_ = RealPlugin.Instance.UpdateRepositoriesAsync([repo], false);
						if (ImGui.MenuItem("编辑"))
							ProxyPlugin.DalamudPlugin.RepoWindow.Open(repo);
						break;
					}
					case RepositoryFolder: {
						if (ImGui.MenuItem("添加")) {
							var repos = new Repository();
							AddRepo(repos, false);
							ProxyPlugin.DalamudPlugin.RepoWindow.Open(repos);
						}
						if (ImGui.MenuItem("全部更新")) _ = RealPlugin.Instance.UpdateAllRepositoriesAsync(false);
						break;
					}
					case Trigger trigger: {
						if (ImGui.MenuItem("执行(忽略条件)"))
							ForceFireTrigger(trigger, TriggerForceTypeEnum.SkipAll);
						if (ImGui.MenuItem("执行(考虑条件)"))
							ForceFireTrigger(trigger, TriggerForceTypeEnum.SkipExceptConditions);
						if (ImGui.MenuItem("编辑触发器"))
							ProxyPlugin.DalamudPlugin.TriggerWindow.Open(trigger);
						break;
					}
					case Folder folder: {
						if (ImGui.MenuItem("编辑"))
							ProxyPlugin.DalamudPlugin.FolderWindow.Open(folder);
						break;
					}
				}
				ImGui.Separator();
				if (ImGui.MenuItem("新建分组")) {
					var target = tag switch {
						Trigger trigger => trigger.Parent,
						Folder folder => folder,
						_ => null
					};
					if (target != null) {
						var f = new Folder();
						target.Folders.Add(f);
						ProxyPlugin.DalamudPlugin.FolderWindow.Open(f);
					}
				}
				if (ImGui.MenuItem("新建触发器")) {
					var target = tag switch {
						Trigger trigger => trigger.Parent,
						Folder folder => folder,
						_ => null
					};
					if (target != null) {
						var t = new Trigger();
						target.Triggers.Add(t);
						ProxyPlugin.DalamudPlugin.TriggerWindow.Open(t);
					}
				}
				ImGui.Separator();
				// if (ImGui.MenuItem("导入"))
				// 	ProxyPlugin.DalamudPlugin.ImportWindow.Open(tag);
				if (ImGui.MenuItem("从剪切板导入(粘贴)"))
					ProxyPlugin.DalamudPlugin.ImportWindow.Import(tag, ImGui.GetClipboardText());
				if (ImGui.MenuItem("导出(复制到剪切板)"))
					ImGui.SetClipboardText(ExportSelection(tag).Serialize());
				ImGui.Separator();
				if (ImGui.MenuItem("移除")) {
					lock (RealPlugin.Instance) {
						switch (tag) {
							case Trigger triggerx:
								RealPlugin.Instance.RemoveTrigger(triggerx);
								triggerx.Parent.Triggers.Remove(triggerx);
								ProxyPlugin.DalamudPlugin.ActionWindow.Open(tag);
								ProxyPlugin.DalamudPlugin.TriggerWindow.Open(tag);
								break;
							case Folder folderx:
								RemoveAllTriggers(folderx);
								folderx.Parent.Folders.Remove(folderx);
								ProxyPlugin.DalamudPlugin.FolderWindow.Open(tag);
								break;
							case Repository repository:
								RemoveRepo(repository.Address);
								ProxyPlugin.DalamudPlugin.RepoWindow.Open(tag);
								break;
						}
					}
				}
				ImGui.EndPopup();
				if (tempStylePopped) ImGui.PushStyleColor(ImGuiCol.Text, ColorGrey);
			}

			if (_nodeCheckedStates[tag] != isChecked) {
				_nodeCheckedStates[tag] = isChecked;
				switch (tag) {
					case Folder folder:
						folder.Enabled = isChecked;
						break;
					case Trigger trigger:
						trigger.Enabled = isChecked;
						break;
					case Repository repo:
						repo.Enabled = isChecked;
						break;
				}
			}
			_nodeExpandedStates[tag] = isExpanded;
			if (isExpanded && renderChildren != null) {
				ImGui.Indent();
				renderChildren();
				ImGui.Unindent();
				ImGui.TreePop();
			}
			if (isDisabled) ImGui.PopStyleColor();
		} catch (Exception e) {
			RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Warning, e.ToString());
		}
	}


	//
	// public void AddMoreRepoCN(bool shouldUpdate = true)
	// {
	// }
	//
	// private void btnOptions_DropDownOpening(object sender, EventArgs e)
	// {
	//     btnActionQueueProcessing.Visible = cfg.DeveloperMode;
	//     btnTryInterrupt.Visible = cfg.DeveloperMode;
	//     btnClearActionQueue.Visible = cfg.DeveloperMode;
	//     btnClearVars.Visible = cfg.DeveloperMode;
	//     btnTestInput.Visible = cfg.DeveloperMode;
	//     btnDeactivateAllAuras.Visible = cfg.DeveloperMode;
	//     toolStripSeparator5.Visible = cfg.DeveloperMode;
	//     btnBenchmark.Visible = cfg.DeveloperMode;
	//     btnViewVariables.Visible = cfg.DeveloperMode;
	//     btnSearch.Visible = cfg.DeveloperMode;
	// }
	//
	// private void ctxFire_Click(object sender, EventArgs e)
	// {
	//     if (treeView1.SelectedNode != null)
	//     {
	//         TreeNode tn = treeView1.SelectedNode;
	//         if (tn.Tag is Trigger)
	//         {
	//             Trigger t = (Trigger)tn.Tag;
	//             ForceFireTrigger(t, Action.TriggerForceTypeEnum.SkipAll);
	//         }
	//     }
	// }
	//
	// private void ctxFireAllowCondition_Click(object sender, EventArgs e)
	// {
	//     if (treeView1.SelectedNode != null)
	//     {
	//         TreeNode tn = treeView1.SelectedNode;
	//         if (tn.Tag is Trigger)
	//         {
	//             Trigger t = (Trigger)tn.Tag;
	//             ForceFireTrigger(t, Action.TriggerForceTypeEnum.SkipExceptConditions);
	//         }
	//     }
	// }
	//
	// private void ForceFireTrigger(Trigger t, Action.TriggerForceTypeEnum force)
	// {
	//     Context ctx = new Context();
	//     ctx.plug = plug;
	//     ctx.testByPlaceholder = false;
	//     ctx.trig = t;
	//     ctx.soundhook = plug.SoundPlaybackSmart;
	//     ctx.ttshook = plug.TtsPlaybackSmart;
	//     ctx.triggered = DateTime.UtcNow;
	//     ctx.force = force;
	//     if ((t._TestInput?.Length ?? 0) == 0)
	//     {
	//         t.Fire(plug, ctx, null);
	//     }
	//     else
	//     {
	//         string[] lines = ctx.EvaluateStringExpression(t.TriggerContextLogger, plug, t._TestInput).Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
	//         force = Action.TriggerForceTypeEnum.SkipActive | Action.TriggerForceTypeEnum.SkipRefire | Action.TriggerForceTypeEnum.SkipParent;
	//         LogEvent.SourceEnum source;
	//         switch (t._Source)
	//         {
	//             case Trigger.TriggerSourceEnum.ACT:
	//                 source = LogEvent.SourceEnum.ACT;
	//                 break;
	//             case Trigger.TriggerSourceEnum.Endpoint:
	//                 source = LogEvent.SourceEnum.Endpoint;
	//                 break;
	//             case Trigger.TriggerSourceEnum.FFXIVNetwork:
	//                 source = LogEvent.SourceEnum.NetworkFFXIV;
	//                 break;
	//             case Trigger.TriggerSourceEnum.Log:
	//                 source = LogEvent.SourceEnum.Log;
	//                 break;
	//             default:
	//             case Trigger.TriggerSourceEnum.None:
	//                 force |= Action.TriggerForceTypeEnum.SkipRegexp;
	//                 source = LogEvent.SourceEnum.Log;
	//                 break;
	//         }
	//         foreach (string line in lines)
	//         {
	//             LogEvent le = new LogEvent() { Text = line, Timestamp = DateTime.Now, TestMode = true, ZoneName = "", ZoneId = null, Source = source };
	//             plug.TestTrigger(t, le, force);
	//         }
	//     }
	// }
	//
	// private void btnCornerPopup_Click(object sender, EventArgs e)
	// {
	//     TabPage tp = (TabPage)btnCornerPopup.Tag;
	//     plug.TabLocateHook(tp);
	// }
	//
	// private void btnSearch_Click(object sender, EventArgs e)
	// {
	//     OpenSearchForm();
	// }
	//
	// private void button1_Click(object sender, EventArgs e)
	// {
	// }
	//
	// internal void LocateTreeNode(TreeNode tn)
	// {
	//     treeView1.SelectedNode = tn;
	//     treeView1.SelectedNode.EnsureVisible();
	//     treeView1.Focus();
	//     FindForm().Focus();
	// }
	//
	// private void ctxReadme_Click(object sender, EventArgs e)
	// {
	//     btnEdit_Click(sender, e);
	// }
	//
	// private void btnUpdateCheck_Click(object sender, EventArgs e)
	// {
	//     plug.CheckForUpdates(isManual: true);
	// }
	//
	// private void timer1_Tick(object sender, EventArgs e)
	// {
	//     RepositoryFolder rfo = (RepositoryFolder)treeView1.Nodes[1].Tag;
	//     List<Repository> upds = new List<Repository>();
	//     foreach (Repository r in rfo.Repositories)
	//     {
	//         if (r.Enabled == true && r.AutoUpdate == true && r.LastUpdatedTrig.AddMinutes(r.UpdateInterval) < DateTime.Now)
	//         {
	//             upds.Add(r);
	//         }
	//     }
	//     if (upds.Count > 0)
	//     {
	//         Task tx = new Task(() =>
	//         {
	//             plug.RepositoryUpdates(upds, false);
	//         });
	//         tx.Start();                
	//     }
	//     if (cfg.AutosaveEnabled == true && plug.lastConfigSave.AddMinutes(cfg.AutosaveInterval) < DateTime.Now)
	//     {
	//         if (plug.configBroken == false)
	//         {
	//             plug.SaveCurrentConfig();
	//         }
	//     }
	// }
}