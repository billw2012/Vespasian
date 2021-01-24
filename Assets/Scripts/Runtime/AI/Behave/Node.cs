// unset

using System;
using System.Collections.Generic;
using System.Linq;

namespace AI.Behave
{
    public abstract class Blackboard {}
    
    public abstract class Node
    {
        public enum Result
        {
            None,
            Success,
            Failure,
            Running,
        }

        public virtual string name => this.GetType().Name; 
        public int tick { get; private set; } = 0;
        public Result lastResult { get; protected set; }
        public Node lastNode { get; protected set; }

        public (Result result, Node node) Update(object blackboard)
        {
            (this.lastResult, this.lastNode) = this.UpdateImpl(blackboard);
            if (this.lastResult == Result.Running)
            {
                ++this.tick;
            }
            else
            {
                this.tick = 0;
            }
            return (this.lastResult, this.lastNode);
        }

        public virtual bool Visit(Func<Node, int, bool> visitor, int depth = 0) => visitor(this, depth);

        protected abstract (Result result, Node node) UpdateImpl(object blackboard);

        public virtual void Reset()
        {
            this.lastResult = Result.None;
            this.tick = 0;
        }
    }

    public abstract class Decorator : Node
    {
        protected Node child;

        public Decorator(Node child)
        {
            this.child = child;
        }
        
        public override bool Visit(Func<Node, int, bool> visitor, int depth = 0)
        {
            return base.Visit(visitor, depth) && this.child.Visit(visitor, depth + 1);
        }
    }

    public class Tree : Node
    {
        private readonly Node root;
        public override string name { get; }

        public Tree(string name, Node root)
        {
            this.name = name;
            this.root = root;
        }
        
        public override bool Visit(Func<Node, int, bool> visitor, int depth = 0) => this.root.Visit(visitor);

        protected override (Result result, Node node) UpdateImpl(object blackboard)
        {
            var (result, node) = this.root.Update(blackboard);
            if (result != Result.Running)
            {
                this.root.Reset();
            }

            return (result, node);
        }
    }
    
    public abstract class ControlFlow : Node
    {
        public override string name { get; }
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
            base.Reset();
            foreach (var child in this.children)
            {
                child.Reset();
            }
        }
        
        public override bool Visit(Func<Node, int, bool> visitor, int depth = 0)
        {
            if (!base.Visit(visitor, depth))
                return false;
            foreach (var child in this.children)
            {
                if (!child.Visit(visitor, depth + 1))
                    return false;
            }
            return true;
        }
    }

    public class Selector : ControlFlow
    {
        public Selector(string name, params Node[] nodes) : base(name, nodes) {}

        protected override (Result result, Node node) UpdateImpl(object blackboard)
        {
            for (int i = 0; i < this.children.Count; i++)
            {
                var t = this.children[i];
                var (result, node) = t.Update(blackboard);
                if (result != Result.Failure)
                {
                    return (result, node);
                }
            }

            return (Result.Failure, this);
        }
    }

    public class Sequence : ControlFlow
    {
        private int childIndex = 0;

        public Sequence(string name, params Node[] nodes) : base(name, nodes) {}

        protected override (Result result, Node node) UpdateImpl(object blackboard)
        {
            for (int i = this.childIndex; i < this.children.Count; i++)
            {
                var (result, node) = this.children[i].Update(blackboard);
                if (result != Result.Success)
                {
                    return (result, node);
                }
            }
            return (Result.Success, this);
        }

        public override void Reset()
        {
            base.Reset();
            this.childIndex = 0;
        }
    }
}