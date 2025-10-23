using PKHeX.Core;
using PluginPile.Common;

namespace PluginPile.TMTool;
public class TMTool : PluginBase {
  public override string Name => nameof(TMTool);
  private BoxManipulator Manipulator = null!;
  private readonly BoxManipType CustomManipType = (BoxManipType)42;
  private readonly ToolStripMenuItem MenuItem;
  private bool IsCompatibleSave => SaveFileEditor.SAV is SAV9SV or SAV8SWSH;

  public TMTool() {
    MenuItem = new ToolStripMenuItem(Language.MenuItemName);
  }

  protected override void HandleSaveLoaded() => MenuItem.Visible = IsCompatibleSave;

  public override void NotifyDisplayLanguageChanged(string language) => SetMenuItems();

  protected override void LoadBoxMenu(ContextMenuStrip boxMenu) {
    Manipulator = GetBoxManipulatorWF();
    boxMenu.Items.Add(MenuItem);
    SetMenuItems();
  }

  private void SetMenuItems() {
    MenuItem.Text = Language.MenuItemName;
    MenuItem.DropDownItems.Clear();

    ToolStripMenuItem addProtectMenuItem = new ToolStripMenuItem(Language.AddProtect);
    BoxManipModify modifyProtect = new BoxManipModify(CustomManipType, pkm => ((PK9)pkm).SetMoveRecordFlag(Constants.TM_PROTECT, true), s => s is SAV9SV);
    addProtectMenuItem.Click += (s, e) => Manipulator.Execute(modifyProtect, SaveFileEditor.CurrentBox, false, false);
    MenuItem.DropDownItems.Add(addProtectMenuItem);

    ToolStripMenuItem learnTMsMenuItem = new ToolStripMenuItem(Language.LearnTMs);
    BoxManipModify modifyTMs = new BoxManipModify(CustomManipType, pkm => LearnTMsFromCurrentMoves((PK9)pkm), s => s is SAV9SV);
    learnTMsMenuItem.Click += (s, e) => Manipulator.Execute(modifyTMs, SaveFileEditor.CurrentBox, false, false);
    MenuItem.DropDownItems.Add(learnTMsMenuItem);
  }

  protected override void LoadContextMenu(ContextMenuStrip contextMenu) {
    contextMenu.Opening += (s, e) => {
      SlotViewInfo<PictureBox> info = GetSenderInfo(ref s!);
      if (IsCompatibleSave && info.IsNonEmptyWriteableBoxSlot()) {
        ToolStripMenuItem tmToolItem = new ToolStripMenuItem(Language.MenuItemName);

        void ApplyChange(Action<PKM> change) {
          int slotIndex = SaveFileEditor.CurrentBox * SaveFileEditor.SAV.BoxSlotCount + info.Slot.Slot;
          PKM mon = SaveFileEditor.SAV.GetBoxSlotAtIndex(slotIndex);
          change(mon);
          SaveFileEditor.SAV.SetBoxSlotAtIndex(mon, slotIndex, EntityImportSettings.None);
        }
        if (SaveFileEditor.SAV is SAV9SV) {
          ToolStripMenuItem addProtect = new ToolStripMenuItem(Language.AddProtect);
          addProtect.Click += (s, e) => ApplyChange(mon => (mon as ITechRecord)?.SetMoveRecordFlag(Constants.TM_PROTECT, true));
          tmToolItem.DropDownItems.Add(addProtect);
        }

        ToolStripMenuItem learnTMsItem = new ToolStripMenuItem(Language.LearnTMs);
        learnTMsItem.Click += (s, e) => ApplyChange(mon => LearnTMsFromCurrentMoves(mon));
        tmToolItem.DropDownItems.Add(learnTMsItem);

        contextMenu.Items.Add(tmToolItem);
        contextMenu.Closing += (s, e) => contextMenu.Items.Remove(tmToolItem);
      }
    };
  }

  // Copied from PKHeX.WinForms.PKMEditor
  private void LearnTMsFromCurrentMoves(PKM pk) {
    Span<ushort> moves = stackalloc ushort[4];
    pk.GetMoves(moves);
    LegalityAnalysis la = new LegalityAnalysis(pk);
    (pk as ITechRecord)?.SetRecordFlags(pk, TechnicalRecordApplicatorOption.LegalAll);
  }
}
