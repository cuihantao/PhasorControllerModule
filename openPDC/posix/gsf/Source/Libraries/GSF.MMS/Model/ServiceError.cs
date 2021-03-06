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
    [ASN1Sequence(Name = "ServiceError", IsSet = false)]
    public class ServiceError : IASN1PreparedElement
    {
        private static readonly IASN1PreparedElementData preparedData = CoderFactory.getInstance().newPreparedElementData(typeof(ServiceError));
        private long additionalCode_;

        private bool additionalCode_present;


        private string additionalDescription_;

        private bool additionalDescription_present;
        private ErrorClassChoiceType errorClass_;


        private ServiceSpecificInfoChoiceType serviceSpecificInfo_;

        private bool serviceSpecificInfo_present;

        [ASN1Element(Name = "errorClass", IsOptional = false, HasTag = true, Tag = 0, HasDefaultValue = false)]
        public ErrorClassChoiceType ErrorClass
        {
            get
            {
                return errorClass_;
            }
            set
            {
                errorClass_ = value;
            }
        }

        [ASN1Integer(Name = "")]
        [ASN1Element(Name = "additionalCode", IsOptional = true, HasTag = true, Tag = 1, HasDefaultValue = false)]
        public long AdditionalCode
        {
            get
            {
                return additionalCode_;
            }
            set
            {
                additionalCode_ = value;
                additionalCode_present = true;
            }
        }

        [ASN1String(Name = "",
            StringType = UniversalTags.VisibleString, IsUCS = false)]
        [ASN1Element(Name = "additionalDescription", IsOptional = true, HasTag = true, Tag = 2, HasDefaultValue = false)]
        public string AdditionalDescription
        {
            get
            {
                return additionalDescription_;
            }
            set
            {
                additionalDescription_ = value;
                additionalDescription_present = true;
            }
        }


        [ASN1Element(Name = "serviceSpecificInfo", IsOptional = true, HasTag = true, Tag = 3, HasDefaultValue = false)]
        public ServiceSpecificInfoChoiceType ServiceSpecificInfo
        {
            get
            {
                return serviceSpecificInfo_;
            }
            set
            {
                serviceSpecificInfo_ = value;
                serviceSpecificInfo_present = true;
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


        public bool isAdditionalCodePresent()
        {
            return additionalCode_present;
        }

        public bool isAdditionalDescriptionPresent()
        {
            return additionalDescription_present;
        }

        public bool isServiceSpecificInfoPresent()
        {
            return serviceSpecificInfo_present;
        }

        [ASN1PreparedElement]
        [ASN1Choice(Name = "errorClass")]
        public class ErrorClassChoiceType : IASN1PreparedElement
        {
            private static IASN1PreparedElementData preparedData = CoderFactory.getInstance().newPreparedElementData(typeof(ErrorClassChoiceType));
            private long access_;
            private bool access_selected;
            private long application_reference_;
            private bool application_reference_selected;
            private long cancel_;
            private bool cancel_selected;
            private long conclude_;
            private bool conclude_selected;


            private long definition_;
            private bool definition_selected;
            private long file_;
            private bool file_selected;
            private long initiate_;
            private bool initiate_selected;
            private long others_;
            private bool others_selected;


            private long resource_;
            private bool resource_selected;


            private long service_;


            private long service_preempt_;
            private bool service_preempt_selected;
            private bool service_selected;


            private long time_resolution_;
            private bool time_resolution_selected;
            private long vmd_state_;
            private bool vmd_state_selected;


            [ASN1Integer(Name = "")]
            [ASN1Element(Name = "vmd-state", IsOptional = false, HasTag = true, Tag = 0, HasDefaultValue = false)]
            public long Vmd_state
            {
                get
                {
                    return vmd_state_;
                }
                set
                {
                    selectVmd_state(value);
                }
            }

            [ASN1Integer(Name = "")]
            [ASN1Element(Name = "application-reference", IsOptional = false, HasTag = true, Tag = 1, HasDefaultValue = false)]
            public long Application_reference
            {
                get
                {
                    return application_reference_;
                }
                set
                {
                    selectApplication_reference(value);
                }
            }

            [ASN1Integer(Name = "")]
            [ASN1Element(Name = "definition", IsOptional = false, HasTag = true, Tag = 2, HasDefaultValue = false)]
            public long Definition
            {
                get
                {
                    return definition_;
                }
                set
                {
                    selectDefinition(value);
                }
            }

            [ASN1Integer(Name = "")]
            [ASN1Element(Name = "resource", IsOptional = false, HasTag = true, Tag = 3, HasDefaultValue = false)]
            public long Resource
            {
                get
                {
                    return resource_;
                }
                set
                {
                    selectResource(value);
                }
            }

            [ASN1Integer(Name = "")]
            [ASN1Element(Name = "service", IsOptional = false, HasTag = true, Tag = 4, HasDefaultValue = false)]
            public long Service
            {
                get
                {
                    return service_;
                }
                set
                {
                    selectService(value);
                }
            }

            [ASN1Integer(Name = "")]
            [ASN1Element(Name = "service-preempt", IsOptional = false, HasTag = true, Tag = 5, HasDefaultValue = false)]
            public long Service_preempt
            {
                get
                {
                    return service_preempt_;
                }
                set
                {
                    selectService_preempt(value);
                }
            }


            [ASN1Integer(Name = "")]
            [ASN1Element(Name = "time-resolution", IsOptional = false, HasTag = true, Tag = 6, HasDefaultValue = false)]
            public long Time_resolution
            {
                get
                {
                    return time_resolution_;
                }
                set
                {
                    selectTime_resolution(value);
                }
            }


            [ASN1Integer(Name = "")]
            [ASN1Element(Name = "access", IsOptional = false, HasTag = true, Tag = 7, HasDefaultValue = false)]
            public long Access
            {
                get
                {
                    return access_;
                }
                set
                {
                    selectAccess(value);
                }
            }


            [ASN1Integer(Name = "")]
            [ASN1Element(Name = "initiate", IsOptional = false, HasTag = true, Tag = 8, HasDefaultValue = false)]
            public long Initiate
            {
                get
                {
                    return initiate_;
                }
                set
                {
                    selectInitiate(value);
                }
            }


            [ASN1Integer(Name = "")]
            [ASN1Element(Name = "conclude", IsOptional = false, HasTag = true, Tag = 9, HasDefaultValue = false)]
            public long Conclude
            {
                get
                {
                    return conclude_;
                }
                set
                {
                    selectConclude(value);
                }
            }


            [ASN1Integer(Name = "")]
            [ASN1Element(Name = "cancel", IsOptional = false, HasTag = true, Tag = 10, HasDefaultValue = false)]
            public long Cancel
            {
                get
                {
                    return cancel_;
                }
                set
                {
                    selectCancel(value);
                }
            }


            [ASN1Integer(Name = "")]
            [ASN1Element(Name = "file", IsOptional = false, HasTag = true, Tag = 11, HasDefaultValue = false)]
            public long File
            {
                get
                {
                    return file_;
                }
                set
                {
                    selectFile(value);
                }
            }


            [ASN1Integer(Name = "")]
            [ASN1Element(Name = "others", IsOptional = false, HasTag = true, Tag = 12, HasDefaultValue = false)]
            public long Others
            {
                get
                {
                    return others_;
                }
                set
                {
                    selectOthers(value);
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


            public bool isVmd_stateSelected()
            {
                return vmd_state_selected;
            }


            public void selectVmd_state(long val)
            {
                vmd_state_ = val;
                vmd_state_selected = true;


                vmd_state_selected = false;

                application_reference_selected = false;

                definition_selected = false;

                resource_selected = false;

                service_selected = false;

                service_preempt_selected = false;

                time_resolution_selected = false;

                access_selected = false;

                initiate_selected = false;

                conclude_selected = false;

                cancel_selected = false;

                file_selected = false;

                others_selected = false;
            }


            public bool isApplication_referenceSelected()
            {
                return application_reference_selected;
            }


            public void selectApplication_reference(long val)
            {
                application_reference_ = val;
                application_reference_selected = true;


                vmd_state_selected = false;

                application_reference_selected = false;

                definition_selected = false;

                resource_selected = false;

                service_selected = false;

                service_preempt_selected = false;

                time_resolution_selected = false;

                access_selected = false;

                initiate_selected = false;

                conclude_selected = false;

                cancel_selected = false;

                file_selected = false;

                others_selected = false;
            }


            public bool isDefinitionSelected()
            {
                return definition_selected;
            }


            public void selectDefinition(long val)
            {
                definition_ = val;
                definition_selected = true;


                vmd_state_selected = false;

                application_reference_selected = false;

                resource_selected = false;

                service_selected = false;

                service_preempt_selected = false;

                time_resolution_selected = false;

                access_selected = false;

                initiate_selected = false;

                conclude_selected = false;

                cancel_selected = false;

                file_selected = false;

                others_selected = false;
            }


            public bool isResourceSelected()
            {
                return resource_selected;
            }


            public void selectResource(long val)
            {
                resource_ = val;
                resource_selected = true;


                vmd_state_selected = false;

                application_reference_selected = false;

                definition_selected = false;

                service_selected = false;

                service_preempt_selected = false;

                time_resolution_selected = false;

                access_selected = false;

                initiate_selected = false;

                conclude_selected = false;

                cancel_selected = false;

                file_selected = false;

                others_selected = false;
            }


            public bool isServiceSelected()
            {
                return service_selected;
            }


            public void selectService(long val)
            {
                service_ = val;
                service_selected = true;


                vmd_state_selected = false;

                application_reference_selected = false;

                definition_selected = false;

                resource_selected = false;

                service_preempt_selected = false;

                time_resolution_selected = false;

                access_selected = false;

                initiate_selected = false;

                conclude_selected = false;

                cancel_selected = false;

                file_selected = false;

                others_selected = false;
            }


            public bool isService_preemptSelected()
            {
                return service_preempt_selected;
            }


            public void selectService_preempt(long val)
            {
                service_preempt_ = val;
                service_preempt_selected = true;


                vmd_state_selected = false;

                application_reference_selected = false;

                definition_selected = false;

                resource_selected = false;

                service_selected = false;

                service_preempt_selected = false;

                time_resolution_selected = false;

                access_selected = false;

                initiate_selected = false;

                conclude_selected = false;

                cancel_selected = false;

                file_selected = false;

                others_selected = false;
            }


            public bool isTime_resolutionSelected()
            {
                return time_resolution_selected;
            }


            public void selectTime_resolution(long val)
            {
                time_resolution_ = val;
                time_resolution_selected = true;


                vmd_state_selected = false;

                application_reference_selected = false;

                definition_selected = false;

                resource_selected = false;

                service_selected = false;

                service_preempt_selected = false;

                time_resolution_selected = false;

                access_selected = false;

                initiate_selected = false;

                conclude_selected = false;

                cancel_selected = false;

                file_selected = false;

                others_selected = false;
            }


            public bool isAccessSelected()
            {
                return access_selected;
            }


            public void selectAccess(long val)
            {
                access_ = val;
                access_selected = true;


                vmd_state_selected = false;

                application_reference_selected = false;

                definition_selected = false;

                resource_selected = false;

                service_selected = false;

                service_preempt_selected = false;

                time_resolution_selected = false;

                initiate_selected = false;

                conclude_selected = false;

                cancel_selected = false;

                file_selected = false;

                others_selected = false;
            }


            public bool isInitiateSelected()
            {
                return initiate_selected;
            }


            public void selectInitiate(long val)
            {
                initiate_ = val;
                initiate_selected = true;


                vmd_state_selected = false;

                application_reference_selected = false;

                definition_selected = false;

                resource_selected = false;

                service_selected = false;

                service_preempt_selected = false;

                time_resolution_selected = false;

                access_selected = false;

                conclude_selected = false;

                cancel_selected = false;

                file_selected = false;

                others_selected = false;
            }


            public bool isConcludeSelected()
            {
                return conclude_selected;
            }


            public void selectConclude(long val)
            {
                conclude_ = val;
                conclude_selected = true;


                vmd_state_selected = false;

                application_reference_selected = false;

                definition_selected = false;

                resource_selected = false;

                service_selected = false;

                service_preempt_selected = false;

                time_resolution_selected = false;

                access_selected = false;

                initiate_selected = false;

                cancel_selected = false;

                file_selected = false;

                others_selected = false;
            }


            public bool isCancelSelected()
            {
                return cancel_selected;
            }


            public void selectCancel(long val)
            {
                cancel_ = val;
                cancel_selected = true;


                vmd_state_selected = false;

                application_reference_selected = false;

                definition_selected = false;

                resource_selected = false;

                service_selected = false;

                service_preempt_selected = false;

                time_resolution_selected = false;

                access_selected = false;

                initiate_selected = false;

                conclude_selected = false;

                file_selected = false;

                others_selected = false;
            }


            public bool isFileSelected()
            {
                return file_selected;
            }


            public void selectFile(long val)
            {
                file_ = val;
                file_selected = true;


                vmd_state_selected = false;

                application_reference_selected = false;

                definition_selected = false;

                resource_selected = false;

                service_selected = false;

                service_preempt_selected = false;

                time_resolution_selected = false;

                access_selected = false;

                initiate_selected = false;

                conclude_selected = false;

                cancel_selected = false;

                others_selected = false;
            }


            public bool isOthersSelected()
            {
                return others_selected;
            }


            public void selectOthers(long val)
            {
                others_ = val;
                others_selected = true;


                vmd_state_selected = false;

                application_reference_selected = false;

                definition_selected = false;

                resource_selected = false;

                service_selected = false;

                service_preempt_selected = false;

                time_resolution_selected = false;

                access_selected = false;

                initiate_selected = false;

                conclude_selected = false;

                cancel_selected = false;

                file_selected = false;
            }
        }

        [ASN1PreparedElement]
        [ASN1Choice(Name = "serviceSpecificInfo")]
        public class ServiceSpecificInfoChoiceType : IASN1PreparedElement
        {
            private static IASN1PreparedElementData preparedData = CoderFactory.getInstance().newPreparedElementData(typeof(ServiceSpecificInfoChoiceType));
            private AdditionalService_Error additionalService_;
            private bool additionalService_selected;
            private ChangeAccessControl_Error changeAccessControl_;
            private bool changeAccessControl_selected;
            private DefineEventEnrollment_Error defineEventEnrollment_Error_;
            private bool defineEventEnrollment_Error_selected;
            private DeleteNamedType_Error deleteNamedType_;
            private bool deleteNamedType_selected;
            private DeleteNamedVariableList_Error deleteNamedVariableList_;
            private bool deleteNamedVariableList_selected;
            private DeleteVariableAccess_Error deleteVariableAccess_;
            private bool deleteVariableAccess_selected;
            private FileRename_Error fileRename_;
            private bool fileRename_selected;
            private ObtainFile_Error obtainFile_;
            private bool obtainFile_selected;
            private Reset_Error reset_;
            private bool reset_selected;
            private Resume_Error resume_;
            private bool resume_selected;


            private Start_Error start_;
            private bool start_selected;


            private Stop_Error stop_;
            private bool stop_selected;

            [ASN1Element(Name = "obtainFile", IsOptional = false, HasTag = true, Tag = 0, HasDefaultValue = false)]
            public ObtainFile_Error ObtainFile
            {
                get
                {
                    return obtainFile_;
                }
                set
                {
                    selectObtainFile(value);
                }
            }

            [ASN1Element(Name = "start", IsOptional = false, HasTag = true, Tag = 1, HasDefaultValue = false)]
            public Start_Error Start
            {
                get
                {
                    return start_;
                }
                set
                {
                    selectStart(value);
                }
            }


            [ASN1Element(Name = "stop", IsOptional = false, HasTag = true, Tag = 2, HasDefaultValue = false)]
            public Stop_Error Stop
            {
                get
                {
                    return stop_;
                }
                set
                {
                    selectStop(value);
                }
            }


            [ASN1Element(Name = "resume", IsOptional = false, HasTag = true, Tag = 3, HasDefaultValue = false)]
            public Resume_Error Resume
            {
                get
                {
                    return resume_;
                }
                set
                {
                    selectResume(value);
                }
            }


            [ASN1Element(Name = "reset", IsOptional = false, HasTag = true, Tag = 4, HasDefaultValue = false)]
            public Reset_Error Reset
            {
                get
                {
                    return reset_;
                }
                set
                {
                    selectReset(value);
                }
            }


            [ASN1Element(Name = "deleteVariableAccess", IsOptional = false, HasTag = true, Tag = 5, HasDefaultValue = false)]
            public DeleteVariableAccess_Error DeleteVariableAccess
            {
                get
                {
                    return deleteVariableAccess_;
                }
                set
                {
                    selectDeleteVariableAccess(value);
                }
            }


            [ASN1Element(Name = "deleteNamedVariableList", IsOptional = false, HasTag = true, Tag = 6, HasDefaultValue = false)]
            public DeleteNamedVariableList_Error DeleteNamedVariableList
            {
                get
                {
                    return deleteNamedVariableList_;
                }
                set
                {
                    selectDeleteNamedVariableList(value);
                }
            }


            [ASN1Element(Name = "deleteNamedType", IsOptional = false, HasTag = true, Tag = 7, HasDefaultValue = false)]
            public DeleteNamedType_Error DeleteNamedType
            {
                get
                {
                    return deleteNamedType_;
                }
                set
                {
                    selectDeleteNamedType(value);
                }
            }


            [ASN1Element(Name = "defineEventEnrollment-Error", IsOptional = false, HasTag = true, Tag = 8, HasDefaultValue = false)]
            public DefineEventEnrollment_Error DefineEventEnrollment_Error
            {
                get
                {
                    return defineEventEnrollment_Error_;
                }
                set
                {
                    selectDefineEventEnrollment_Error(value);
                }
            }


            [ASN1Element(Name = "fileRename", IsOptional = false, HasTag = true, Tag = 9, HasDefaultValue = false)]
            public FileRename_Error FileRename
            {
                get
                {
                    return fileRename_;
                }
                set
                {
                    selectFileRename(value);
                }
            }


            [ASN1Element(Name = "additionalService", IsOptional = false, HasTag = true, Tag = 10, HasDefaultValue = false)]
            public AdditionalService_Error AdditionalService
            {
                get
                {
                    return additionalService_;
                }
                set
                {
                    selectAdditionalService(value);
                }
            }


            [ASN1Element(Name = "changeAccessControl", IsOptional = false, HasTag = true, Tag = 11, HasDefaultValue = false)]
            public ChangeAccessControl_Error ChangeAccessControl
            {
                get
                {
                    return changeAccessControl_;
                }
                set
                {
                    selectChangeAccessControl(value);
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


            public bool isObtainFileSelected()
            {
                return obtainFile_selected;
            }


            public void selectObtainFile(ObtainFile_Error val)
            {
                obtainFile_ = val;
                obtainFile_selected = true;


                start_selected = false;

                stop_selected = false;

                resume_selected = false;

                reset_selected = false;

                deleteVariableAccess_selected = false;

                deleteNamedVariableList_selected = false;

                deleteNamedType_selected = false;

                defineEventEnrollment_Error_selected = false;

                fileRename_selected = false;

                additionalService_selected = false;

                changeAccessControl_selected = false;
            }


            public bool isStartSelected()
            {
                return start_selected;
            }


            public void selectStart(Start_Error val)
            {
                start_ = val;
                start_selected = true;


                obtainFile_selected = false;

                stop_selected = false;

                resume_selected = false;

                reset_selected = false;

                deleteVariableAccess_selected = false;

                deleteNamedVariableList_selected = false;

                deleteNamedType_selected = false;

                defineEventEnrollment_Error_selected = false;

                fileRename_selected = false;

                additionalService_selected = false;

                changeAccessControl_selected = false;
            }


            public bool isStopSelected()
            {
                return stop_selected;
            }


            public void selectStop(Stop_Error val)
            {
                stop_ = val;
                stop_selected = true;


                obtainFile_selected = false;

                start_selected = false;

                resume_selected = false;

                reset_selected = false;

                deleteVariableAccess_selected = false;

                deleteNamedVariableList_selected = false;

                deleteNamedType_selected = false;

                defineEventEnrollment_Error_selected = false;

                fileRename_selected = false;

                additionalService_selected = false;

                changeAccessControl_selected = false;
            }


            public bool isResumeSelected()
            {
                return resume_selected;
            }


            public void selectResume(Resume_Error val)
            {
                resume_ = val;
                resume_selected = true;


                obtainFile_selected = false;

                start_selected = false;

                stop_selected = false;

                reset_selected = false;

                deleteVariableAccess_selected = false;

                deleteNamedVariableList_selected = false;

                deleteNamedType_selected = false;

                defineEventEnrollment_Error_selected = false;

                fileRename_selected = false;

                additionalService_selected = false;

                changeAccessControl_selected = false;
            }


            public bool isResetSelected()
            {
                return reset_selected;
            }


            public void selectReset(Reset_Error val)
            {
                reset_ = val;
                reset_selected = true;


                obtainFile_selected = false;

                start_selected = false;

                stop_selected = false;

                resume_selected = false;

                deleteVariableAccess_selected = false;

                deleteNamedVariableList_selected = false;

                deleteNamedType_selected = false;

                defineEventEnrollment_Error_selected = false;

                fileRename_selected = false;

                additionalService_selected = false;

                changeAccessControl_selected = false;
            }


            public bool isDeleteVariableAccessSelected()
            {
                return deleteVariableAccess_selected;
            }


            public void selectDeleteVariableAccess(DeleteVariableAccess_Error val)
            {
                deleteVariableAccess_ = val;
                deleteVariableAccess_selected = true;


                obtainFile_selected = false;

                start_selected = false;

                stop_selected = false;

                resume_selected = false;

                reset_selected = false;

                deleteNamedVariableList_selected = false;

                deleteNamedType_selected = false;

                defineEventEnrollment_Error_selected = false;

                fileRename_selected = false;

                additionalService_selected = false;

                changeAccessControl_selected = false;
            }


            public bool isDeleteNamedVariableListSelected()
            {
                return deleteNamedVariableList_selected;
            }


            public void selectDeleteNamedVariableList(DeleteNamedVariableList_Error val)
            {
                deleteNamedVariableList_ = val;
                deleteNamedVariableList_selected = true;


                obtainFile_selected = false;

                start_selected = false;

                stop_selected = false;

                resume_selected = false;

                reset_selected = false;

                deleteVariableAccess_selected = false;

                deleteNamedType_selected = false;

                defineEventEnrollment_Error_selected = false;

                fileRename_selected = false;

                additionalService_selected = false;

                changeAccessControl_selected = false;
            }


            public bool isDeleteNamedTypeSelected()
            {
                return deleteNamedType_selected;
            }


            public void selectDeleteNamedType(DeleteNamedType_Error val)
            {
                deleteNamedType_ = val;
                deleteNamedType_selected = true;


                obtainFile_selected = false;

                start_selected = false;

                stop_selected = false;

                resume_selected = false;

                reset_selected = false;

                deleteVariableAccess_selected = false;

                deleteNamedVariableList_selected = false;

                defineEventEnrollment_Error_selected = false;

                fileRename_selected = false;

                additionalService_selected = false;

                changeAccessControl_selected = false;
            }


            public bool isDefineEventEnrollment_ErrorSelected()
            {
                return defineEventEnrollment_Error_selected;
            }


            public void selectDefineEventEnrollment_Error(DefineEventEnrollment_Error val)
            {
                defineEventEnrollment_Error_ = val;
                defineEventEnrollment_Error_selected = true;


                obtainFile_selected = false;

                start_selected = false;

                stop_selected = false;

                resume_selected = false;

                reset_selected = false;

                deleteVariableAccess_selected = false;

                deleteNamedVariableList_selected = false;

                deleteNamedType_selected = false;

                defineEventEnrollment_Error_selected = false;

                fileRename_selected = false;

                additionalService_selected = false;

                changeAccessControl_selected = false;
            }


            public bool isFileRenameSelected()
            {
                return fileRename_selected;
            }


            public void selectFileRename(FileRename_Error val)
            {
                fileRename_ = val;
                fileRename_selected = true;


                obtainFile_selected = false;

                start_selected = false;

                stop_selected = false;

                resume_selected = false;

                reset_selected = false;

                deleteVariableAccess_selected = false;

                deleteNamedVariableList_selected = false;

                deleteNamedType_selected = false;

                defineEventEnrollment_Error_selected = false;

                additionalService_selected = false;

                changeAccessControl_selected = false;
            }


            public bool isAdditionalServiceSelected()
            {
                return additionalService_selected;
            }


            public void selectAdditionalService(AdditionalService_Error val)
            {
                additionalService_ = val;
                additionalService_selected = true;


                obtainFile_selected = false;

                start_selected = false;

                stop_selected = false;

                resume_selected = false;

                reset_selected = false;

                deleteVariableAccess_selected = false;

                deleteNamedVariableList_selected = false;

                deleteNamedType_selected = false;

                defineEventEnrollment_Error_selected = false;

                fileRename_selected = false;

                changeAccessControl_selected = false;
            }


            public bool isChangeAccessControlSelected()
            {
                return changeAccessControl_selected;
            }


            public void selectChangeAccessControl(ChangeAccessControl_Error val)
            {
                changeAccessControl_ = val;
                changeAccessControl_selected = true;


                obtainFile_selected = false;

                start_selected = false;

                stop_selected = false;

                resume_selected = false;

                reset_selected = false;

                deleteVariableAccess_selected = false;

                deleteNamedVariableList_selected = false;

                deleteNamedType_selected = false;

                defineEventEnrollment_Error_selected = false;

                fileRename_selected = false;

                additionalService_selected = false;
            }
        }
    }
}