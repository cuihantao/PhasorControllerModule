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
    [ASN1Sequence(Name = "DefineNamedVariable_Request", IsSet = false)]
    public class DefineNamedVariable_Request : IASN1PreparedElement
    {
        private static readonly IASN1PreparedElementData preparedData = CoderFactory.getInstance().newPreparedElementData(typeof(DefineNamedVariable_Request));
        private Address address_;


        private TypeSpecification typeSpecification_;

        private bool typeSpecification_present;
        private ObjectName variableName_;

        [ASN1Element(Name = "variableName", IsOptional = false, HasTag = true, Tag = 0, HasDefaultValue = false)]
        public ObjectName VariableName
        {
            get
            {
                return variableName_;
            }
            set
            {
                variableName_ = value;
            }
        }

        [ASN1Element(Name = "address", IsOptional = false, HasTag = true, Tag = 1, HasDefaultValue = false)]
        public Address Address
        {
            get
            {
                return address_;
            }
            set
            {
                address_ = value;
            }
        }

        [ASN1Element(Name = "typeSpecification", IsOptional = true, HasTag = true, Tag = 2, HasDefaultValue = false)]
        public TypeSpecification TypeSpecification
        {
            get
            {
                return typeSpecification_;
            }
            set
            {
                typeSpecification_ = value;
                typeSpecification_present = true;
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

        public bool isTypeSpecificationPresent()
        {
            return typeSpecification_present;
        }
    }
}