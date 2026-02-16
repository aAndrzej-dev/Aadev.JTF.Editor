using System;
using Aadev.JTF.Editor.EditorItems;
using Aadev.JTF.Editor.ViewModels;

namespace Aadev.JTF.Editor;

internal delegate void EditorItemEventHandler(EditorItem sender);
internal delegate void EditorItemEventHandler<T>(EditorItem sender, T args) where T : EventArgs;
internal delegate void JtNodeViewModelEventHandler(JtNodeViewModel sender);
public delegate void JtNodeViewModelEventHandler<T>(JtNodeViewModel sender, T args) where T : EventArgs;
internal delegate void JtTwinFamilyViewModelEventHandler<T>(JtTwinFamilyViewModel sender, T args) where T : EventArgs;