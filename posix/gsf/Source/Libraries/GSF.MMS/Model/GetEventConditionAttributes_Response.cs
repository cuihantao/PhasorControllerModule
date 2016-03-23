//
// This file was generated by the BinaryNotes compiler.
// See http://bnotes.sourceforge.net 
// Any modifications to this file will be lost upon recompilation of the source ASN.1. 
//

using System.Runtime.CompilerServices;
using GSF.ASN1;
using GSF.ASN1.Attributes;
using GSF.ASN1.Coders;
using GSF.ASN1.Types;

namespace GSF.MMS.Model
{
    
    [ASN1PreparedElement]
    [ASN1Sequence(Name = "GetEventConditionAttributes_Response", IsSet = false)]
    public class GetEventConditionAttributes_Response : IASN1PreparedElement
    {
        private static readonly IASN1PreparedElementData preparedData = CoderFactory.getInstance().newPreparedElementData(typeof(GetEventConditionAttributes_Response));
        private Identifier accessControlList_;

        private bool accessControlList_present;
        private bool alarmSummaryReports_;
        private EC_Class class__;
        private Unsigned32 evaluationInterval_;

        private bool evaluationInterval_present;
        private bool mmsDeletable_;
        private MonitoredVariableChoiceType monitoredVariable_;

        private bool monitoredVariable_present;
        private Priority priority_;
        private Unsigned8 severity_;

        [ASN1Boolean(Name = "")]
        [ASN1Element(Name = "mmsDeletable", IsOptional = false, HasTag = true, Tag = 0, HasDefaultValue = true)]
        public bool MmsDeletable
        {
            get
            {
                return mmsDeletable_;
            }
            set
            {
                mmsDeletable_ = value;
            }
        }


        [ASN1Element(Name = "class", IsOptional = false, HasTag = true, Tag = 1, HasDefaultValue = false)]
        public EC_Class Class_
        {
            get
            {
                return class__;
            }
            set
            {
                class__ = value;
            }
        }


        [ASN1Element(Name = "priority", IsOptional = false, HasTag = true, Tag = 2, HasDefaultValue = true)]
        public Priority Priority
        {
            get
            {
                return priority_;
            }
            set
            {
                priority_ = value;
            }
        }


        [ASN1Element(Name = "severity", IsOptional = false, HasTag = true, Tag = 3, HasDefaultValue = true)]
        public Unsigned8 Severity
        {
            get
            {
                return severity_;
            }
            set
            {
                severity_ = value;
            }
        }


        [ASN1Boolean(Name = "")]
        [ASN1Element(Name = "alarmSummaryReports", IsOptional = false, HasTag = true, Tag = 4, HasDefaultValue = true)]
        public bool AlarmSummaryReports
        {
            get
            {
                return alarmSummaryReports_;
            }
            set
            {
                alarmSummaryReports_ = value;
            }
        }


        [ASN1Element(Name = "monitoredVariable", IsOptional = true, HasTag = true, Tag = 6, HasDefaultValue = false)]
        public MonitoredVariableChoiceType MonitoredVariable
        {
            get
            {
                return monitoredVariable_;
            }
            set
            {
                monitoredVariable_ = value;
                monitoredVariable_present = true;
            }
        }


        [ASN1Element(Name = "evaluationInterval", IsOptional = true, HasTag = true, Tag = 7, HasDefaultValue = false)]
        public Unsigned32 EvaluationInterval
        {
            get
            {
                return evaluationInterval_;
            }
            set
            {
                evaluationInterval_ = value;
                evaluationInterval_present = true;
            }
        }


        [ASN1Element(Name = "accessControlList", IsOptional = true, HasTag = true, Tag = 8, HasDefaultValue = false)]
        public Identifier AccessControlList
        {
            get
            {
                return accessControlList_;
            }
            set
            {
                accessControlList_ = value;
                accessControlList_present = true;
            }
        }

        public void initWithDefaults()
        {
            bool param_MmsDeletable =
                false;
            MmsDeletable = param_MmsDeletable;
            Priority param_Priority =
                new Priority(64);
            Priority = param_Priority;
            Unsigned8 param_Severity =
                new Unsigned8(64);
            Severity = param_Severity;
            bool param_AlarmSummaryReports =
                false;
            AlarmSummaryReports = param_AlarmSummaryReports;
        }

        public IASN1PreparedElementData PreparedData
        {
            get
            {
                return preparedData;
            }
        }


        public bool isMonitoredVariablePresent()
        {
            return monitoredVariable_present;
        }

        public bool isEvaluationIntervalPresent()
        {
            return evaluationInterval_present;
        }

        public bool isAccessControlListPresent()
        {
            return accessControlList_present;
        }

        [ASN1PreparedElement]
        [ASN1Choice(Name = "monitoredVariable")]
        public class MonitoredVariableChoiceType : IASN1PreparedElement
        {
            private static IASN1PreparedElementData preparedData = CoderFactory.getInstance().newPreparedElementData(typeof(MonitoredVariableChoiceType));
            private NullObject undefined_;
            private bool undefined_selected;
            private VariableSpecification variableReference_;
            private bool variableReference_selected;


            [ASN1Element(Name = "variableReference", IsOptional = false, HasTag = true, Tag = 0, HasDefaultValue = false)]
            public VariableSpecification VariableReference
            {
                get
                {
                    return variableReference_;
                }
                set
                {
                    selectVariableReference(value);
                }
            }


            [ASN1Null(Name = "undefined")]
            [ASN1Element(Name = "undefined", IsOptional = false, HasTag = true, Tag = 1, HasDefaultValue = false)]
            public NullObject Undefined
            {
                get
                {
                    return undefined_;
                }
                set
                {
                    selectUndefined(value);
                }
            }

            public void initWithDefaults()
            {
            }

            public IASN1PreparedElementData PreparedData
            {
                get
                {
                    return preparedData;
                }
            }


            public bool isVariableReferenceSelected()
            {
                return variableReference_selected;
            }


            public void selectVariableReference(VariableSpecification val)
            {
                variableReference_ = val;
                variableReference_selected = true;


                undefined_selected = false;
            }


            public bool isUndefinedSelected()
            {
                return undefined_selected;
            }


            public void selectUndefined()
            {
                selectUndefined(new NullObject());
            }


            public void selectUndefined(NullObject val)
            {
                undefined_ = val;
                undefined_selected = true;


                variableReference_selected = false;
            }
        }
    }
}