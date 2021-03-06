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
    [ASN1Sequence(Name = "Initiate_ResponsePDU", IsSet = false)]
    public class Initiate_ResponsePDU : IASN1PreparedElement
    {
        private static readonly IASN1PreparedElementData preparedData = CoderFactory.getInstance().newPreparedElementData(typeof(Initiate_ResponsePDU));
        private InitResponseDetailSequenceType initResponseDetail_;
        private Integer32 localDetailCalled_;

        private bool localDetailCalled_present;
        private Integer8 negotiatedDataStructureNestingLevel_;

        private bool negotiatedDataStructureNestingLevel_present;
        private Integer16 negotiatedMaxServOutstandingCalled_;


        private Integer16 negotiatedMaxServOutstandingCalling_;

        [ASN1Element(Name = "localDetailCalled", IsOptional = true, HasTag = true, Tag = 0, HasDefaultValue = false)]
        public Integer32 LocalDetailCalled
        {
            get
            {
                return localDetailCalled_;
            }
            set
            {
                localDetailCalled_ = value;
                localDetailCalled_present = true;
            }
        }

        [ASN1Element(Name = "negotiatedMaxServOutstandingCalling", IsOptional = false, HasTag = true, Tag = 1, HasDefaultValue = false)]
        public Integer16 NegotiatedMaxServOutstandingCalling
        {
            get
            {
                return negotiatedMaxServOutstandingCalling_;
            }
            set
            {
                negotiatedMaxServOutstandingCalling_ = value;
            }
        }


        [ASN1Element(Name = "negotiatedMaxServOutstandingCalled", IsOptional = false, HasTag = true, Tag = 2, HasDefaultValue = false)]
        public Integer16 NegotiatedMaxServOutstandingCalled
        {
            get
            {
                return negotiatedMaxServOutstandingCalled_;
            }
            set
            {
                negotiatedMaxServOutstandingCalled_ = value;
            }
        }


        [ASN1Element(Name = "negotiatedDataStructureNestingLevel", IsOptional = true, HasTag = true, Tag = 3, HasDefaultValue = false)]
        public Integer8 NegotiatedDataStructureNestingLevel
        {
            get
            {
                return negotiatedDataStructureNestingLevel_;
            }
            set
            {
                negotiatedDataStructureNestingLevel_ = value;
                negotiatedDataStructureNestingLevel_present = true;
            }
        }


        [ASN1Element(Name = "initResponseDetail", IsOptional = false, HasTag = true, Tag = 4, HasDefaultValue = false)]
        public InitResponseDetailSequenceType InitResponseDetail
        {
            get
            {
                return initResponseDetail_;
            }
            set
            {
                initResponseDetail_ = value;
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

        public bool isLocalDetailCalledPresent()
        {
            return localDetailCalled_present;
        }

        public bool isNegotiatedDataStructureNestingLevelPresent()
        {
            return negotiatedDataStructureNestingLevel_present;
        }

        [ASN1PreparedElement]
        [ASN1Sequence(Name = "initResponseDetail", IsSet = false)]
        public class InitResponseDetailSequenceType : IASN1PreparedElement
        {
            private static IASN1PreparedElementData preparedData = CoderFactory.getInstance().newPreparedElementData(typeof(InitResponseDetailSequenceType));
            private AdditionalCBBOptions additionalCbbSupportedCalled_;
            private AdditionalSupportOptions additionalSupportedCalled_;
            private ParameterSupportOptions negotiatedParameterCBB_;
            private Integer16 negotiatedVersionNumber_;
            private string privilegeClassIdentityCalled_;
            private ServiceSupportOptions servicesSupportedCalled_;

            [ASN1Element(Name = "negotiatedVersionNumber", IsOptional = false, HasTag = true, Tag = 0, HasDefaultValue = false)]
            public Integer16 NegotiatedVersionNumber
            {
                get
                {
                    return negotiatedVersionNumber_;
                }
                set
                {
                    negotiatedVersionNumber_ = value;
                }
            }


            [ASN1Element(Name = "negotiatedParameterCBB", IsOptional = false, HasTag = true, Tag = 1, HasDefaultValue = false)]
            public ParameterSupportOptions NegotiatedParameterCBB
            {
                get
                {
                    return negotiatedParameterCBB_;
                }
                set
                {
                    negotiatedParameterCBB_ = value;
                }
            }


            [ASN1Element(Name = "servicesSupportedCalled", IsOptional = false, HasTag = true, Tag = 2, HasDefaultValue = false)]
            public ServiceSupportOptions ServicesSupportedCalled
            {
                get
                {
                    return servicesSupportedCalled_;
                }
                set
                {
                    servicesSupportedCalled_ = value;
                }
            }


            [ASN1Element(Name = "additionalSupportedCalled", IsOptional = true, HasTag = true, Tag = 3, HasDefaultValue = false)]
            public AdditionalSupportOptions AdditionalSupportedCalled
            {
                get
                {
                    return additionalSupportedCalled_;
                }
                set
                {
                    additionalSupportedCalled_ = value;
                }
            }


            [ASN1Element(Name = "additionalCbbSupportedCalled", IsOptional = true, HasTag = true, Tag = 4, HasDefaultValue = false)]
            public AdditionalCBBOptions AdditionalCbbSupportedCalled
            {
                get
                {
                    return additionalCbbSupportedCalled_;
                }
                set
                {
                    additionalCbbSupportedCalled_ = value;
                }
            }


            [ASN1String(Name = "",
                StringType = UniversalTags.VisibleString, IsUCS = false)]
            [ASN1Element(Name = "privilegeClassIdentityCalled", IsOptional = true, HasTag = true, Tag = 5, HasDefaultValue = false)]
            public string PrivilegeClassIdentityCalled
            {
                get
                {
                    return privilegeClassIdentityCalled_;
                }
                set
                {
                    privilegeClassIdentityCalled_ = value;
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