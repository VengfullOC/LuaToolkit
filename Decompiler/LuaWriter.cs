﻿using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Models;
using LuaSharpVM.Core;
using LuaSharpVM.Disassembler;

namespace LuaSharpVM.Decompiler
{
    public class LuaWriter
    {
        private LuaDecoder Decoder;

        private Dictionary<int, int> UsedConstants; // to definde locals
        private List<LuaScriptLine> LuaScriptLines;

        public string LuaScript
        {
            get { return GetScript(); }
        }

        public LuaWriter(ref LuaDecoder decoder)
        {
            this.Decoder = decoder;
            this.LuaScriptLines = new List<LuaScriptLine>();
            WriteFunction(this.Decoder.File.Function);
            for (int i = 0; i < this.Decoder.File.Function.Functions.Count; i++)
                WriteFunction(this.Decoder.File.Function.Functions[i], 2);
        }


        private void WriteFunction(LuaFunction func, int dpth = 0)
        {
            for(int i = 0; i < func.Instructions.Count; i++)
            {
                this.LuaScriptLines.Add(new LuaScriptLine(func.Instructions[i], ref this.Decoder, ref func)
                {
                    Number = i,
                    Depth = dpth
                });
            }
        }

        private string GetScript()
        {
            string result = "";
            foreach (var l in LuaScriptLines)
                result += l.Text;
            return result;
        }
    }

    public class LuaScriptLine
    {
        public int Depth;
        private int number;
        public int Number // Line number OR index?
        {
            set 
            {
                number = value;
                NumberEnd = value; // + 1;
            }
            get { return number; }
        }

        public int NumberEnd; // in case we need more

        private OpcodeType OpType;

        private LuaDecoder Decoder;
        private LuaFunction Func;
        private LuaInstruction Instr;

        private string Op1; // opperands ;D
        private string Op2;
        private string Op3;

        public string Text
        {
            get { return ToString(); }
        }

        public LuaScriptLine(LuaInstruction instr, ref LuaDecoder decoder, ref LuaFunction func)
        {
            this.Instr = instr;
            this.Func = func;
            SetType();
            SetMain();
        }

        private void SetMain()
        {
            switch (this.Instr.OpCode)
            {
                case LuaOpcode.MOVE:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = " = ";
                    this.Op3 = WriteIndex(Instr.B);
                    break;
                case LuaOpcode.LOADK:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = " = ";
                    this.Op3 = GetConstant(Instr.Bx);
                    break;
                case LuaOpcode.LOADBOOL:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = " = ";
                    this.Op3 = Instr.B != 0 ? "true" : "false";
                    break;
                case LuaOpcode.LOADNIL:
                    for (int i = Instr.A; i < Instr.B + 1; ++i)
                    {
                        this.Op1 += $"{WriteIndex(i)} = nil; "; // TODO: turn into new class?
                        this.NumberEnd++;
                    }   
                    break;
                case LuaOpcode.GETUPVAL:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = " = ";
                    this.Op3 = $"upvalue[{Instr.B}]";
                    break;
                case LuaOpcode.GETGLOBAL:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = " = ";
                    this.Op3 = $"_G[{GetConstant(Instr.B)}]";
                    break;
                case LuaOpcode.GETTABLE:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = " = ";
                    this.Op3 = $"var{Instr.B}[{WriteIndex(Instr.C)}]";
                    break;
                case LuaOpcode.SETGLOBAL:
                    this.Op1 = $"_G[{WriteIndex(Instr.Bx)}]";
                    this.Op2 = " = ";
                    this.Op3 = $"var{Instr.A}";
                    break;
                case LuaOpcode.SETUPVAL:
                    this.Op1 = $"upvalue[{WriteIndex(Instr.B)}]";
                    this.Op2 = " = ";
                    this.Op3 = $"var[{GetConstant(Instr.A)}]";
                    break;
                case LuaOpcode.SETTABLE:
                    this.Op1 = $"{WriteIndex(Instr.A)}[{WriteIndex(Instr.B)}]";
                    this.Op2 = " = ";
                    this.Op3 = WriteIndex(Instr.C);
                    break;
                case LuaOpcode.NEWTABLE:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = " = ";
                    this.Op3 = "{{}}";
                    break;
                case LuaOpcode.SELF:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = " = ";
                    this.Op3 = $"var{Instr.B}; {WriteIndex(Instr.A)} = var{Instr.B}[{WriteIndex(Instr.C)}]";
                    // TODO fix, multiline?
                    this.NumberEnd++;
                    break;
                case LuaOpcode.ADD:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = $" = var{Instr.B}";
                    this.Op3 = $" + var{Instr.C}";
                    break;
                 case LuaOpcode.SUB:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = $" = var{Instr.B}";
                    this.Op3 = $" + var{Instr.C}";
                    break;
                 case LuaOpcode.MUL:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = $" = var{Instr.B}";
                    this.Op3 = $" * var{Instr.C}";
                    break;
                 case LuaOpcode.DIV:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = $" = var{Instr.B}";
                    this.Op3 = $" / var{Instr.C}";
                    break;
                 case LuaOpcode.MOD:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = $" = var{Instr.B}";
                    this.Op3 = $" % var{Instr.C}";
                    break;
                 case LuaOpcode.POW:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = $" = var{Instr.B}";
                    this.Op3 = $" ^ var{Instr.C}";
                    break;
                 case LuaOpcode.UNM:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = $" = ";
                    this.Op3 = $"-var{Instr.B}";
                    break;
                 case LuaOpcode.NOT:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = $" = ";
                    this.Op3 = $"not var{Instr.B}";
                    break;
                case LuaOpcode.LEN:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = $" = ";
                    this.Op3 = $"#var{Instr.B}";
                    break;
                case LuaOpcode.CONCAT:
                    this.Op1 = $"{WriteIndex(Instr.A)} = ";
                    for(int i = Instr.B; i <= Instr.C; ++i) 
                    {
                        this.Op2 += $"{WriteIndex(i)}";
                        if (i < Instr.C)
                            this.Op2 += " .. ";
                    }  
                    break;
                case LuaOpcode.JMP:
                    // Do nothing ;D
                    break;
                case LuaOpcode.EQ:
                    this.Op1 = $"if ({WriteIndex(Instr.B)}";
                    this.Op2 = " == ";
                    this.Op3 = $"{WriteIndex(Instr.C)}) ~= {Instr.A} then";
                    break;
                case LuaOpcode.LT:
                    this.Op1 = $"if ({WriteIndex(Instr.B)}";
                    this.Op2 = " < ";
                    this.Op3 = $"{WriteIndex(Instr.C)}) ~= {Instr.A} then";
                    break;
                case LuaOpcode.LE:
                    this.Op1 = $"if ({WriteIndex(Instr.B)}";
                    this.Op2 = " <= ";
                    this.Op3 = $"{WriteIndex(Instr.C)}) ~= {Instr.A} then";
                    break;
                case LuaOpcode.TEST:
                    this.Op1 = $"if not var{Instr.A}";
                    this.Op2 = " <=> ";
                    this.Op3 = $"{Instr.C} then";
                    break;
                case LuaOpcode.TESTSET:
                    this.Op1 = $"if var{Instr.B} <=> {Instr.C} then; ";
                    this.Op2 = $"var{Instr.A} = ";
                    this.Op3 = $"var{Instr.B}; end";
                    break;
                // CALL
                // TAILCALL
                // RETURN
                // FORLOOP
                // TFORLOOP
                case LuaOpcode.SETLIST:
                    this.Op1 = $"{WriteIndex(Instr.A)} = {{";
                    for(int i = 1; i <= Instr.B; i++)
                    {
                        this.Op2 += $"{WriteIndex(Instr.A+i)}";
                        if (i < Instr.B)
                            this.Op2 += ", ";
                    }
                    this.Op3 = "}}";
                    break;
                // CLOSE
                // CLOSURE
                // VARAG
                default:
                    this.Op1 = "unk";
                    this.Op2 = "_";
                    this.Op3 = Instr.OpCode.ToString();
                    break;
                    // ez
            }
        }

        private void SetType()
        {
            switch (this.Instr.OpCode)
            {
                case LuaOpcode.LOADK:
                case LuaOpcode.GETGLOBAL:
                case LuaOpcode.SETGLOBAL:
                case LuaOpcode.CLOSURE:
                    this.OpType = OpcodeType.ABx;
                    break;
                case LuaOpcode.FORLOOP:
                case LuaOpcode.FORPREP:
                case LuaOpcode.JMP:
                    this.OpType = OpcodeType.AsBx;
                    break;
                default:
                    this.OpType = OpcodeType.ABC;
                    break;
            }
        }

        private string GetConstant(int index)
        {
            return this.Func.Constants[index].ToString();
        }

        private string WriteIndex(int value)
        {
            bool constant = false;
            int index = ToIndex(value, out constant);

            if (constant)
                return this.Func.Constants[index].ToString();
            else
            {
                // TODO: check if local and not yet used!
                return "var" + index;
            }
        }

        private int ToIndex(int value, out bool isConstant)
        {
            // this is the logic from lua's source code (lopcodes.h)
            if (isConstant = (value & 1 << 8) != 0)
                return value & ~(1 << 8);
            else
                return value;
        }

        public override string ToString()
        {
            string tab = new string(' ', Depth); // NOTE: singple space for debugging
            return $"{tab}{Op1}{Op2}{Op3}\r\n";
        }
    }
}