namespace Model;

#region Enum ECadOption ---------------------------------------------------------------------------
public enum ECadOption { Pick, Line, Rectangle, Circle, Square, PLine, Plane }
#endregion

#region Enum ETransform ---------------------------------------------------------------------------
public enum ETransform { Translate, Mirror, Rotate, Scale }
#endregion

[Flags]
public enum ERender : ulong { None = 0, Ortho = 1UL << 0, Grid = 1UL << 1, Snap = 1UL << 2 }

public enum EQuadrant { I, II, III, IV }