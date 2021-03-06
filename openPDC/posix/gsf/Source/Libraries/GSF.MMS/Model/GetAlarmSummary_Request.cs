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
    [ASN1Sequence(Name = "GetAlarmSummary_Request", IsSet = false)]
    public class GetAlarmSummary_Request : IASN1PreparedElement
    {
        private static readonly IASN1PreparedElementData preparedData = CoderFactory.getInstance().newPreparedElementData(typeof(GetAlarmSummary_Request));
        private long acknowledgementFilter_;
        private bool activeAlarmsOnly_;
        private ObjectName continueAfter_;

        private bool continueAfter_present;
        private bool enrollmentsOnly_;
        private SeverityFilterSequenceType severityFilter_;

        [ASN1Boolean(Name = "")]
        [ASN1Element(Name = "enrollmentsOnly", IsOptional = false, HasTag = true, Tag = 0, HasDefaultValue = true)]
        public bool EnrollmentsOnly
        {
            get
            {
                return enrollmentsOnly_;
            }
            set
            {
                enrollmentsOnly_ = value;
            }
        }


        [ASN1Boolean(Name = "")]
        [ASN1Element(Name = "activeAlarmsOnly", IsOptional = false, HasTag = true, Tag = 1, HasDefaultValue = true)]
        public bool ActiveAlarmsOnly
        {
            get
            {
                return activeAlarmsOnly_;
            }
            set
            {
                activeAlarmsOnly_ = value;
            }
        }


        [ASN1Integer(Name = "")]
        [ASN1Element(Name = "acknowledgementFilter", IsOptional = false, HasTag = true, Tag = 2, HasDefaultValue = true)]
        public long AcknowledgementFilter
        {
            get
            {
                return acknowledgementFilter_;
            }
            set
            {
                acknowledgementFilter_ = value;
            }
        }


        [ASN1Element(Name = "severityFilter", IsOptional = false, HasTag = true, Tag = 3, HasDefaultValue = true)]
        public SeverityFilterSequenceType SeverityFilter
        {
            get
            {
                return severityFilter_;
            }
            set
            {
                severityFilter_ = value;
            }
        }


        [ASN1Element(Name = "continueAfter", IsOptional = true, HasTag = true, Tag = 5, HasDefaultValue = false)]
        public ObjectName ContinueAfter
        {
            get
            {
                return continueAfter_;
            }
            set
            {
                continueAfter_ = value;
                continueAfter_present = true;
            }
        }


        public void initWithDefaults()
        {
            bool param_EnrollmentsOnly =
                false;
            EnrollmentsOnly = param_EnrollmentsOnly;
            bool param_ActiveAlarmsOnly =
                false;
            ActiveAlarmsOnly = param_ActiveAlarmsOnly;
            long param_AcknowledgementFilter =
                0;
            AcknowledgementFilter = param_AcknowledgementFilter;
            SeverityFilterSequenceType param_SeverityFilter =
                new SeverityFilterSequenceType();
            {
                param_SeverityFilter.MostSevere = new Unsigned8(
                    0)
                    ;

                param_SeverityFilter.LeastSevere = new Unsigned8(
                    127)
                    ;
            }
            ;
            SeverityFilter = param_SeverityFilter;
        }


        public IASN1PreparedElementData PreparedData
        {
            get
            {
                return preparedData;
            }
        }

        public bool isContinueAfterPresent()
        {
            return continueAfter_present;
        }

        [ASN1PreparedElement]
        [ASN1Sequence(Name = "severityFilter", IsSet = false)]
        public class SeverityFilterSequenceType : IASN1PreparedElement
        {
            private static IASN1PreparedElementData preparedData = CoderFactory.getInstance().newPreparedElementData(typeof(SeverityFilterSequenceType));
            private Unsigned8 leastSevere_;
            private Unsigned8 mostSevere_;

            [ASN1Element(Name = "mostSevere", IsOptional = false, HasTag = true, Tag = 0, HasDefaultValue = false)]
            public Unsigned8 MostSevere
            {
                get
                {
                    return mostSevere_;
                }
                set
                {
                    mostSevere_ = value;
                }
            }


            [ASN1Element(Name = "leastSevere", IsOptional = false, HasTag = true, Tag = 1, HasDefaultValue = false)]
            public Unsigned8 LeastSevere
            {
                get
                {
                    return leastSevere_;
                }
                set
                {
                    leastSevere_ = value;
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
        }
    }
}