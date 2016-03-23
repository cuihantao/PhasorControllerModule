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

namespace GSF.MMS.Model
{
    
    [ASN1PreparedElement]
    [ASN1Choice(Name = "DeleteEventEnrollment_Request")]
    public class DeleteEventEnrollment_Request : IASN1PreparedElement
    {
        private static readonly IASN1PreparedElementData preparedData = CoderFactory.getInstance().newPreparedElementData(typeof(DeleteEventEnrollment_Request));
        private ObjectName ea_;
        private bool ea_selected;
        private ObjectName ec_;
        private bool ec_selected;
        private ICollection<ObjectName> specific_;
        private bool specific_selected;


        [ASN1SequenceOf(Name = "specific", IsSetOf = false)]
        [ASN1Element(Name = "specific", IsOptional = false, HasTag = true, Tag = 0, HasDefaultValue = false)]
        public ICollection<ObjectName> Specific
        {
            get
            {
                return specific_;
            }
            set
            {
                selectSpecific(value);
            }
        }


        [ASN1Element(Name = "ec", IsOptional = false, HasTag = true, Tag = 1, HasDefaultValue = false)]
        public ObjectName Ec
        {
            get
            {
                return ec_;
            }
            set
            {
                selectEc(value);
            }
        }


        [ASN1Element(Name = "ea", IsOptional = false, HasTag = true, Tag = 2, HasDefaultValue = false)]
        public ObjectName Ea
        {
            get
            {
                return ea_;
            }
            set
            {
                selectEa(value);
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


        public bool isSpecificSelected()
        {
            return specific_selected;
        }


        public void selectSpecific(ICollection<ObjectName> val)
        {
            specific_ = val;
            specific_selected = true;


            ec_selected = false;

            ea_selected = false;
        }


        public bool isEcSelected()
        {
            return ec_selected;
        }


        public void selectEc(ObjectName val)
        {
            ec_ = val;
            ec_selected = true;


            specific_selected = false;

            ea_selected = false;
        }


        public bool isEaSelected()
        {
            return ea_selected;
        }


        public void selectEa(ObjectName val)
        {
            ea_ = val;
            ea_selected = true;


            specific_selected = false;

            ec_selected = false;
        }
    }
}