using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace itestall
{
    public class ItestallTreeNode : TreeNodeBase<ItestallTreeNode>
    {
        /// <summary>
        /// ノードの名前フィールド
        /// </summary>
        protected string name = null;

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        /// <summary>
        /// ノードのタイプフィールド
        /// </summary>
        protected string type = null;

        public string Type
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
            }
        }

        /// <summary>
        /// ノードの種別フィールド
        /// </summary>
        protected string kind = null;

        public string Kind
        {
            get
            {
                return kind;
            }
            set
            {
                kind = value;
            }
        }

        /// <summary>
        /// ノードのコンストラクタ
        /// </summary>
        public ItestallTreeNode() {}

    }

}
