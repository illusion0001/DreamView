using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace DreamView
{
    class MTreeView : TreeView
    {

    }

    class MPanel : Panel
    {
        public void setBackground(bool on)
        {
            SetStyle(System.Windows.Forms.ControlStyles.Opaque, on);
            SetStyle(System.Windows.Forms.ControlStyles.AllPaintingInWmPaint, on);
        }
    }
    class MButton : Button
    {
        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                    return true;
                default:
                    return base.IsInputKey(keyData);
            }
        }
    }
}
