//
// This file was generated by the BinaryNotes compiler.
// See http://bnotes.sourceforge.net 
// Any modifications to this file will be lost upon recompilation of the source ASN.1. 
//

using System.Runtime.CompilerServices;
using GSF.ASN1;
using GSF.ASN1.Attributes;
using GSF.ASN1.Coders;

namespace GSF.MMS.Model
{
    
    [ASN1PreparedElement]
    [ASN1Sequence(Name = "DefineEventCondition_Request", IsSet = false)]
    public class DefineEventCondition_Request : IASN1PreparedElement
    {
        private static readonly IASN1PreparedElementData preparedData = CoderFactory.getInstance().newPreparedElementData(typeof(DefineEventCondition_Request));
        private bool alarmSummaryReports_;

        private bool alarmSummaryReports_present;
        private EC_Class class__;
        private Unsigned32 evaluationInterval_;

        private bool evaluationInterval_present;
        private ObjectName eventConditionName_;
        private VariableSpecification monitoredVariable_;

        private bool monitoredVariable_present;
        private Priority priority_;
        private Unsigned8 severity_;

        [ASN1Element(Name = "eventConditionName", IsOptional = false, HasTag = true, Tag = 0, HasDefaultValue = false)]
        public ObjectName EventConditionName
        {
            get
            {
                return eventConditionName_;
            }
            set
            {
                eventConditionName_ = value;
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
        [ASN1Element(Name = "alarmSummaryReports", IsOptional = true, HasTag = true, Tag = 4, HasDefaultValue = false)]
        public bool AlarmSummaryReports
        {
            get
            {
                return alarmSummaryReports_;
            }
            set
            {
                alarmSummaryReports_ = value;
                alarmSummaryReports_present = true;
            }
        }


        [ASN1Element(Name = "monitoredVariable", IsOptional = true, HasTag = true, Tag = 6, HasDefaultValue = false)]
        public VariableSpecification MonitoredVariable
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

        public void initWithDefaults()
        {
            Priority param_Priority =
                new Priority(64);
            Priority = param_Priority;
            Unsigned8 param_Severity =
                new Unsigned8(64);
            Severity = param_Severity;
        }

        public IASN1PreparedElementData PreparedData
        {
            get
            {
                return preparedData;
            }
        }


        public bool isAlarmSummaryReportsPresent()
        {
            return alarmSummaryReports_present;
        }

        public bool isMonitoredVariablePresent()
        {
            return monitoredVariable_present;
        }

        public bool isEvaluationIntervalPresent()
        {
            return evaluationInterval_present;
        }
    }
}