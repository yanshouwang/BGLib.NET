using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace BGLib.LegacyWPF.ViewModels
{
    class TreeNode
    {
        public TreeNode(object @object, IList<TreeNode> nodes)
        {
            Object = @object;
            Nodes = nodes;
        }

        public object Object { get; }
        public IList<TreeNode> Nodes { get; }
    }
}
