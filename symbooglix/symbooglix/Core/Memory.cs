using System.Collections.Generic;
using Microsoft.Boogie;
using System.Diagnostics;

namespace symbooglix
{
    public class Memory
    {
        public Memory()
        {
            stack = new List<StackFrame>();
            globals = new List<MemoryObject>();
        }

        public bool dump()
        {
            // TODO:
            return true;
        }

        public void popStackFrame()
        {
            stack.RemoveAt(stack.Count - 1);
        }

        public List<StackFrame> stack;
        public List<MemoryObject> globals;
    }

    public class StackFrame
    {
        public List<MemoryObject> locals;
        public Implementation procedure;
        private BlockCmdIterator BCI;
        public IEnumerator<Absy> currentInstruction;

        public StackFrame(Implementation procedure)
        {
            locals = new List<MemoryObject>();
            this.procedure = procedure;
            transferToBlock(procedure.Blocks[0]);
        }

        public Block currentBlock
        {
            get;
            private set;
        }

        public void transferToBlock(Block BB)
        {
            // Check if BB is in procedure
            Debug.Assert(procedure.Blocks.Contains(BB));

            currentBlock = BB;
            BCI = new BlockCmdIterator(currentBlock);
            currentInstruction = BCI.GetEnumerator();
        }
    }

    public class MemoryObject
    {
        public MemoryObject() { }
    }
}

