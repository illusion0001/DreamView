using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DreamView
{
    partial class MeshSelect : Form
    {
        public MeshSelect(MFrame root)
        {
            InitializeComponent();
            tree.BeginUpdate();
            tree.Nodes.Clear();
            treeAdd(root, tree.Nodes);
            tree.EndUpdate();
        }

        private void treeAdd(MFrame cur,TreeNodeCollection tcol)
        {
            TreeNode node = tcol.Add(cur.Name);
            node.Checked = true;
            if (cur.FrameFirstChild != null)
                treeAdd((MFrame)cur.FrameFirstChild, node.Nodes);
            if (cur.FrameSibling != null)
                treeAdd((MFrame)cur.FrameSibling, tcol);
            if (cur.MeshContainer != null)
            {                
                node.Tag = cur.MeshContainer;
                ((MMeshContainer)cur.MeshContainer).mark = true;
                node.Checked = true;
            }
        }

        private void CheckAllChildNodes(TreeNode treeNode, bool nodeChecked)
        {
            foreach (TreeNode node in treeNode.Nodes)
            {
                node.Checked = nodeChecked;
                if (node.Tag != null)
                    ((MMeshContainer)node.Tag).mark = node.Checked;
                if (node.Nodes.Count > 0)
                {
                    this.CheckAllChildNodes(node, nodeChecked);
                }
            }
        }

        private void tree_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Action != TreeViewAction.Unknown)
            {
                if (e.Node.Tag != null)
                    ((MMeshContainer)e.Node.Tag).mark = e.Node.Checked;            
                if (e.Node.Nodes.Count > 0)
                    this.CheckAllChildNodes(e.Node, e.Node.Checked);                
            }
        }

        private void btOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}