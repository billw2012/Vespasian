// unset

using System.Collections.Generic;
using System.Linq;

namespace AI.Behave
{
    public abstract class Blackboard {}
    
    public abstract class Node
    {
        public enum Result
        {
            Success,
            Failure,
            Running
        }

        public abstract Result Update(object blackboard);

        public virtual void Reset() {}
    }

    public class Tree : Node
    {
        private readonly Node root;
        private string name;

        public Tree(string name, Node root)
        {
            this.name = name;
            this.root = root;
        }

        public override Result Update(object blackboard)
        {
            var result = this.root.Update(blackboard);
            if (result != Result.Running)
            {
                this.root.Reset();
            }

            return result;
        }
    }
    
    public abstract class ControlFlow : Node
    {
        private string name;
        protected readonly IList<Node> children;

        protected ControlFlow(string name, IEnumerable<Node> children)
        {
            this.name = name;
            this.children = children.ToList();
        }

        public ControlFlow Add(Node node)
        {
            this.children.Add(node);
            // Fluent style
            return this;
        }
        
        public override void Reset()
        {
            foreach (var child in this.children)
            {
                child.Reset();
            }
        }
    }

    public class Selector : ControlFlow
    {
        public Selector(string name, params Node[] nodes) : base(name, nodes) {}

        public override Result Update(object blackboard)
        {
            for (int i = 0; i < this.children.Count; i++)
            {
                var result = this.children[i].Update(blackboard);
                if (result != Result.Failure)
                {
                    return result;
                }
            }
            return Result.Failure;
        }
    }

    public class Sequence : ControlFlow
    {
        private int childIndex = 0;

        public Sequence(string name, params Node[] nodes) : base(name, nodes) {}

        public override Result Update(object blackboard)
        {
            for (int i = this.childIndex; i < this.children.Count; i++)
            {
                var result = this.children[i].Update(blackboard);
                if (result != Result.Success)
                {
                    return result;
                }
            }
            return Result.Success;
        }

        public override void Reset()
        {
            this.childIndex = 0;
            base.Reset();
        }
    }
}