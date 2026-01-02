using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;
using Triggernometry.Localization;

namespace Triggernometry.Core.Conditions
{

    public sealed class ConditionGroup : ConditionComponent
    {

        public enum CndGroupingEnum
        {
            And = 1,
            Or = 2,
            Xor = 3,
            Not = 4
        }

        [XmlIgnore]
        private CndGroupingEnum _Grouping = CndGroupingEnum.Or;
        [XmlAttribute]
        public CndGroupingEnum Grouping
        {
            get
            {
                return _Grouping;
            }
            set
            {
                if (value != _Grouping)
                {
                    _Grouping = value;
                    TriggerOnPropertyChange();
                }
            }
        }

        [XmlElement(typeof(ConditionGroup))]
        [XmlElement(typeof(ConditionSingle))]
        public List<ConditionComponent> Children { get; set; } = new List<ConditionComponent>();

        public void AddChild(ConditionComponent re)
        {
            re.Parent = this;
            Children.Add(re);
        }

        public void RemoveChild(ConditionComponent re)
        {
            Children.Remove(re);
            re.Parent = null;
        }

        public override string ToString()
        {
            switch (Grouping)
            {
                case CndGroupingEnum.And:
                    return I18n.Translate("internal/ConditionGroup/and", "All conditions must be true");
                case CndGroupingEnum.Or:
                    return I18n.Translate("internal/ConditionGroup/or", "At least one condition must be true");
                case CndGroupingEnum.Xor:
                    return I18n.Translate("internal/ConditionGroup/xor", "Only one condition must be true");
                case CndGroupingEnum.Not:
                    return I18n.Translate("internal/ConditionGroup/not", "None of the conditions may be true");
            }
            return I18n.Translate("internal/ConditionGroup/unknown", "Unknown grouping value '{0}'", Grouping);
        }

        internal override ConditionComponent Duplicate()
        {
            ConditionGroup cg = new ConditionGroup();
            cg.Enabled = Enabled;
            cg.Grouping = Grouping;
            foreach (ConditionComponent cc in Children)
            {
                cg.AddChild(cc.Duplicate());
            }
            return cg;
        }

        internal static void RebuildParentage(ConditionGroup cg)
        {
            foreach (ConditionComponent cc in cg.Children)
            {
                cc.Parent = cg;
                if (cc is ConditionGroup)
                {
                    RebuildParentage((ConditionGroup)cc);
                }
            }
        }

        internal override bool CheckCondition(Context ctx, Context.LoggerDelegate logger, object o)
        {
            if (Children.Count == 0 && Parent == null)
            {
                return true;
            }
            switch (Grouping)
            {
                case CndGroupingEnum.And:
                    {
                        bool actives = false;
                        foreach (ConditionComponent cc in Children)
                        {
                            if (!cc.Enabled)
                            {
                                if (Debugger.IsAttached)
                                {
                                    Debug.WriteLine("AND: Condition '" + cc + "' not enabled");
                                }
                                continue;
                            }
                            actives = true;
                            if (!cc.CheckCondition(ctx, logger, o))
                            {
                                if (Debugger.IsAttached)
                                {
                                    Debug.WriteLine("AND: Condition '" + cc + "' was false");
                                }
                                return false;
                            }
                        }
                        if (!actives)
                        {
                            if (Debugger.IsAttached)
                            {
                                Debug.WriteLine("AND: No actives found");
                            }
                            return false;
                        }
                    }
                    break;
                case CndGroupingEnum.Or:
                    {
                        foreach (ConditionComponent cc in Children)
                        {
                            if (!cc.Enabled)
                            {
                                if (Debugger.IsAttached)
                                {
                                    Debug.WriteLine("OR: Condition '" + cc + "' not enabled");
                                }
                                continue;
                            }
                            if (cc.CheckCondition(ctx, logger, o))
                            {
                                if (Debugger.IsAttached)
                                {
                                    Debug.WriteLine("OR: Condition '" + cc + "' was true");
                                }
                                return true;
                            }
                        }
                    }
                    break;
                case CndGroupingEnum.Xor:
                    {
                        bool truefound = false;
                        foreach (ConditionComponent cc in Children)
                        {
                            if (!cc.Enabled)
                            {
                                if (Debugger.IsAttached)
                                {
                                    Debug.WriteLine("XOR: Condition '" + cc + "' not enabled");
                                }
                                continue;
                            }
                            if (cc.CheckCondition(ctx, logger, o))
                            {
                                if (truefound)
                                {
                                    if (Debugger.IsAttached)
                                    {
                                        Debug.WriteLine("XOR: Condition '" + cc + "' was true as well");
                                    }
                                    return false;
                                }
                                if (Debugger.IsAttached)
                                {
                                    Debug.WriteLine("XOR: Condition '" + cc + "' was true");
                                }
                                truefound = true;
                            }
                        }
                        if (Debugger.IsAttached)
                        {
                            Debug.WriteLine("XOR: Condition on '" + ToString() + "' " + (truefound ? "passed" : "did not pass"));
                        }
                        return truefound;
                    }
                case CndGroupingEnum.Not:
                    {
                        foreach (ConditionComponent cc in Children)
                        {
                            if (!cc.Enabled)
                            {
                                if (Debugger.IsAttached)
                                {
                                    Debug.WriteLine("NOT: Condition '" + cc + "' not enabled");
                                }
                                continue;
                            }
                            if (cc.CheckCondition(ctx, logger, o))
                            {
                                if (Debugger.IsAttached)
                                {
                                    Debug.WriteLine("NOT: Condition '" + cc + "' was true");
                                }
                                return false;
                            }
                        }
                        if (Debugger.IsAttached)
                        {
                            Debug.WriteLine("NOT: Condition on '" + ToString() + "' passed");
                        }
                        return true;
                    }
            }
            if (Debugger.IsAttached)
            {
                Debug.WriteLine("GENERAL: Condition on '" + ToString() + "' " + (Children.Count > 0 && Grouping == CndGroupingEnum.And ? "passed" : "did not pass"));
            }
            return Children.Count > 0 && Grouping == CndGroupingEnum.And; 
        }

    }

}
