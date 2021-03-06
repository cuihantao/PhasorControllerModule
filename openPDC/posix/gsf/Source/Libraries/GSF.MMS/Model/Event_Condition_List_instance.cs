//
// This file was generated by the BinaryNotes compiler.
// See http://bnotes.sourceforge.net 
// Any modifications to this file will be lost upon recompilation of the source ASN.1. 
//

using System.Runtime.CompilerServices;
using System.Collections.Generic;
using GSF.ASN1;
using GSF.ASN1.Attributes;
using GSF.ASN1.Coders;
using GSF.ASN1.Types;

namespace GSF.MMS.Model
{
    
    [ASN1PreparedElement]
    [ASN1Sequence(Name = "Event_Condition_List_instance", IsSet = false)]
    public class Event_Condition_List_instance : IASN1PreparedElement
    {
        private static readonly IASN1PreparedElementData preparedData = CoderFactory.getInstance().newPreparedElementData(typeof(Event_Condition_List_instance));
        private DefinitionChoiceType definition_;
        private ObjectName name_;

        [ASN1Element(Name = "name", IsOptional = false, HasTag = true, Tag = 0, HasDefaultValue = false)]
        public ObjectName Name
        {
            get
            {
                return name_;
            }
            set
            {
                name_ = value;
            }
        }


        [ASN1Element(Name = "definition", IsOptional = false, HasTag = false, HasDefaultValue = false)]
        public DefinitionChoiceType Definition
        {
            get
            {
                return definition_;
            }
            set
            {
                definition_ = value;
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

        [ASN1PreparedElement]
        [ASN1Choice(Name = "definition")]
        public class DefinitionChoiceType : IASN1PreparedElement
        {
            private static IASN1PreparedElementData preparedData = CoderFactory.getInstance().newPreparedElementData(typeof(DefinitionChoiceType));
            private DetailsSequenceType details_;
            private bool details_selected;
            private ObjectIdentifier reference_;
            private bool reference_selected;


            [ASN1ObjectIdentifier(Name = "")]
            [ASN1Element(Name = "reference", IsOptional = false, HasTag = true, Tag = 1, HasDefaultValue = false)]
            public ObjectIdentifier Reference
            {
                get
                {
                    return reference_;
                }
                set
                {
                    selectReference(value);
                }
            }


            [ASN1Element(Name = "details", IsOptional = false, HasTag = true, Tag = 2, HasDefaultValue = false)]
            public DetailsSequenceType Details
            {
                get
                {
                    return details_;
                }
                set
                {
                    selectDetails(value);
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


            public bool isReferenceSelected()
            {
                return reference_selected;
            }


            public void selectReference(ObjectIdentifier val)
            {
                reference_ = val;
                reference_selected = true;


                details_selected = false;
            }


            public bool isDetailsSelected()
            {
                return details_selected;
            }


            public void selectDetails(DetailsSequenceType val)
            {
                details_ = val;
                details_selected = true;


                reference_selected = false;
            }

            [ASN1PreparedElement]
            [ASN1Sequence(Name = "details", IsSet = false)]
            public class DetailsSequenceType : IASN1PreparedElement
            {
                private static IASN1PreparedElementData preparedData = CoderFactory.getInstance().newPreparedElementData(typeof(DetailsSequenceType));
                private Access_Control_List_instance accessControl_;
                private ICollection<Event_Condition_List_instance> eventConditionLists_;


                private ICollection<Event_Condition_instance> eventConditions_;
                private ICollection<Event_Condition_List_instance> referencingEventConditionLists_;

                [ASN1Element(Name = "accessControl", IsOptional = false, HasTag = true, Tag = 3, HasDefaultValue = false)]
                public Access_Control_List_instance AccessControl
                {
                    get
                    {
                        return accessControl_;
                    }
                    set
                    {
                        accessControl_ = value;
                    }
                }

                [ASN1SequenceOf(Name = "eventConditions", IsSetOf = false)]
                [ASN1Element(Name = "eventConditions", IsOptional = false, HasTag = true, Tag = 4, HasDefaultValue = false)]
                public ICollection<Event_Condition_instance> EventConditions
                {
                    get
                    {
                        return eventConditions_;
                    }
                    set
                    {
                        eventConditions_ = value;
                    }
                }


                [ASN1SequenceOf(Name = "eventConditionLists", IsSetOf = false)]
                [ASN1Element(Name = "eventConditionLists", IsOptional = false, HasTag = true, Tag = 5, HasDefaultValue = false)]
                public ICollection<Event_Condition_List_instance> EventConditionLists
                {
                    get
                    {
                        return eventConditionLists_;
                    }
                    set
                    {
                        eventConditionLists_ = value;
                    }
                }


                [ASN1SequenceOf(Name = "referencingEventConditionLists", IsSetOf = false)]
                [ASN1Element(Name = "referencingEventConditionLists", IsOptional = false, HasTag = true, Tag = 6, HasDefaultValue = false)]
                public ICollection<Event_Condition_List_instance> ReferencingEventConditionLists
                {
                    get
                    {
                        return referencingEventConditionLists_;
                    }
                    set
                    {
                        referencingEventConditionLists_ = value;
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
}