using Foundation;
using ObjCRuntime;
using UIKit;

namespace Kmila;

/// <summary>
/// Mac Catalyst application delegate for the Kmila MAUI app.
/// Registered with the Objective-C runtime as "AppDelegate" and serves as the
/// entry point for the Mac Catalyst application lifecycle.
/// </summary>
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    /// <summary>
    /// Creates the MAUI application instance by delegating to <see cref="MauiProgram.CreateMauiApp"/>.
    /// </summary>
    /// <returns>A configured <see cref="MauiApp"/> instance.</returns>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    /// <summary>
    /// Registers the editor's keyboard commands as native menu items. On Mac
    /// Catalyst a ⌘-key equivalent is resolved against the menu/responder chain
    /// BEFORE the key event reaches the WKWebView, and an unmatched ⌘-combo is
    /// swallowed — so in-app shortcuts only fire if there's a matching menu
    /// command, and ones whose ⌘-key is already claimed by a built-in command
    /// (New Window ⌘N, the system Cut/Copy/Paste that never reach the WebView)
    /// are stolen. We therefore first REMOVE the conflicting built-ins, then add
    /// our own. Clipboard is special: the WKWebView doesn't deliver the system
    /// edit actions into the web content (documented limitation — Monaco #2205,
    /// Uno #9877), so our ⌘X/⌘C/⌘V/⌘A bridge text through UIPasteboard + JS
    /// instead (see KmilaMacSaveBridge).
    /// </summary>
    public override void BuildMenu(IUIMenuBuilder builder)
    {
        base.BuildMenu(builder);

        try
        {
            // Free ⌘-keys the system would otherwise grab. Each removal is
            // independent so one unknown identifier can't block the others.
            TryRemove(builder, UIMenuIdentifier.StandardEdit); // system Cut/Copy/Paste (don't reach Monaco)
            TryRemove(builder, UIMenuIdentifier.NewScene);      // "New Window" on ⌘N

            // App commands (File menu).
            var fileGroup = UIMenu.Create(
                string.Empty, null, UIMenuIdentifier.None, UIMenuOptions.DisplayInline,
                new UIMenuElement[]
                {
                    Key("New File",     new Selector("kmilaNew:"),   "n"),
                    Key("Save",         new Selector("kmilaSave:"),  "s"),
                    Key("Build",        new Selector("kmilaBuild:"), "b"),
                    Key("Run",          new Selector("kmilaRun:"),   "\r"),
                    Key("Split Editor", new Selector("kmilaSplit:"), "t"),
                });
            builder.InsertChildMenuAtStart(fileGroup, UIMenuIdentifier.File.GetConstant());

            // Clipboard (Edit menu) — our selectors bridge through UIPasteboard
            // + JS because the WebView's own cut/copy/paste don't work here.
            var editGroup = UIMenu.Create(
                string.Empty, null, UIMenuIdentifier.None, UIMenuOptions.DisplayInline,
                new UIMenuElement[]
                {
                    Key("Cut",        new Selector("kmilaCut:"),       "x"),
                    Key("Copy",       new Selector("kmilaCopy:"),      "c"),
                    Key("Paste",      new Selector("kmilaPaste:"),     "v"),
                    Key("Select All", new Selector("kmilaSelectAll:"), "a"),
                });
            builder.InsertChildMenuAtStart(editGroup, UIMenuIdentifier.Edit.GetConstant());
        }
        catch
        {
            // Menu APIs vary across OS versions — missing menu items are a minor
            // regression (the shortcut just won't fire), never a crash.
        }
    }

    private static void TryRemove(IUIMenuBuilder builder, UIMenuIdentifier id)
    {
        try { builder.RemoveMenu(id.GetConstant()); } catch { /* identifier not present on this OS */ }
    }

    /// <summary>Builds a ⌘-modified key command bound to <paramref name="action"/>.</summary>
    private static UIKeyCommand Key(string title, Selector action, string input) =>
        UIKeyCommand.Create(new NSString(title), null, action, new NSString(input), UIKeyModifierFlags.Command, null);

    /// <summary>
    /// Ctrl variants of the same commands (⌘ versions live in the menus above).
    /// Registered as app-wide key commands — no menu items — so both ⌘ and Ctrl
    /// work on macOS. Deliberately NO Ctrl+A: that stays macOS line-start
    /// navigation. Note Ctrl+B / Ctrl+N / Ctrl+V likewise override the
    /// emacs-style back-char / next-line / page-down bindings in text fields,
    /// which is the intended trade for having those as command shortcuts.
    /// </summary>
    public override UIKeyCommand[]? KeyCommands => _ctrlCommands ??= BuildCtrlCommands();

    private UIKeyCommand[]? _ctrlCommands;

    private static UIKeyCommand[] BuildCtrlCommands() => new[]
    {
        CtrlKey("n",  new Selector("kmilaNew:")),
        CtrlKey("s",  new Selector("kmilaSave:")),
        CtrlKey("b",  new Selector("kmilaBuild:")),
        CtrlKey("\r", new Selector("kmilaRun:")),
        CtrlKey("t",  new Selector("kmilaSplit:")),
        CtrlKey("x",  new Selector("kmilaCut:")),
        CtrlKey("c",  new Selector("kmilaCopy:")),
        CtrlKey("v",  new Selector("kmilaPaste:")),
    };

    private static UIKeyCommand CtrlKey(string input, Selector action)
    {
        var cmd = UIKeyCommand.Create(new NSString(input), UIKeyModifierFlags.Control, action);
        // Beat macOS's built-in Ctrl text bindings (emacs-style) for these keys.
        cmd.WantsPriorityOverSystemBehavior = true;
        return cmd;
    }

    /// <summary>Enables the editor commands whenever the menu system queries them.</summary>
    public override bool CanPerform(Selector action, NSObject? withSender)
    {
        switch (action?.Name)
        {
            case "kmilaNew:":
            case "kmilaSave:":
            case "kmilaBuild:":
            case "kmilaRun:":
            case "kmilaSplit:":
            case "kmilaCut:":
            case "kmilaCopy:":
            case "kmilaPaste:":
            case "kmilaSelectAll:":
                return true;
            default:
                return base.CanPerform(action, withSender);
        }
    }

    [Export("kmilaNew:")]       public void KmilaNew(UIKeyCommand command)       => KmilaMacSaveBridge.Run("new");
    [Export("kmilaSave:")]      public void KmilaSave(UIKeyCommand command)      => KmilaMacSaveBridge.Run("save");
    [Export("kmilaBuild:")]     public void KmilaBuild(UIKeyCommand command)     => KmilaMacSaveBridge.Run("build");
    [Export("kmilaRun:")]       public void KmilaRun(UIKeyCommand command)       => KmilaMacSaveBridge.Run("run");
    [Export("kmilaSplit:")]     public void KmilaSplit(UIKeyCommand command)     => KmilaMacSaveBridge.Run("split");
    [Export("kmilaCut:")]       public void KmilaCut(UIKeyCommand command)       => KmilaMacSaveBridge.Cut();
    [Export("kmilaCopy:")]      public void KmilaCopy(UIKeyCommand command)      => KmilaMacSaveBridge.Copy();
    [Export("kmilaPaste:")]     public void KmilaPaste(UIKeyCommand command)     => KmilaMacSaveBridge.Paste();
    [Export("kmilaSelectAll:")] public void KmilaSelectAll(UIKeyCommand command) => KmilaMacSaveBridge.SelectAll();
}
