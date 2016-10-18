using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.ModelConfiguration;

namespace LTSSWebService.Models
{
    public class Screening
    {
        public bool? AplicantInterestedInMDCPIndicator { get; set; }

        public bool? ApplicantAutismOrPervasiveDisorderIndicator { get; set; }

        public bool? ApplicantCaregiverForExistingConditionsIndicator { get; set; }

        public string ApplicantCaregiverNeedsInformationIndicator { get; set; }

        public bool? ApplicantCognitiveImpairmentIndicator { get; set; }

        public bool? ApplicantDeafAndBlindIndicator { get; set; }

        public bool? ApplicantDevelopmentalDisabilityIndicator { get; set; }

        public string ApplicantDroppedActivitiesAndInterestsIndicator { get; set; }

        public string ApplicantFeelsFullOfEnergyIndicator { get; set; }

        public string ApplicantFeelsLifeIsEmptyIndicator { get; set; }

        public string ApplicantGetsCareFromPersonWhoNeedsHelpIndicator { get; set; }

        public bool? ApplicantGetsCareIndicator { get; set; }

        public bool? ApplicantHasConsentToReleaseOfInformationIndicator { get; set; }

        public bool? ApplicantHasConsumedAlcoholInPast30DaysIndicator { get; set; }

        public bool? ApplicantHasDiabetesMedicationInPast30DaysIndicator { get; set; }

        public bool? ApplicantHasMedicationForAnxietyInPast30DaysIndicator { get; set; }

        public bool? ApplicantHasMedicationForHeartDiseasesOrBloodPressureInPast30DaysIndicator { get; set; }

        public bool? ApplicantHasMedicationForSleepInPast30DaysIndicator { get; set; }

        public bool? ApplicantHasNarcoticsPrescriptionInPast30DaysIndicator { get; set; }

        public string ApplicantHasQuestionsAboutMedicareOrLTCInsuranceIndicator { get; set; }

        public bool? ApplicantHasTriCareInsurance { get; set; }

        public bool? ApplicantIntellectualDisabilityIndicator { get; set; }

        public bool? ApplicantInterestedInCLASSIndicator { get; set; }

        public bool? ApplicantInterestedInPACEIndicator { get; set; }

        public bool? ApplicantInterestedInSTARPLUSIndicator { get; set; }

        public bool? ApplicantInterestednDBMDIndicator { get; set; }

        public string ApplicantLikelyhoodOfMovingToNursingFacilityCode { get; set; }

        public bool? ApplicantMentalHealthDiagnosisIndicator { get; set; }

        public bool? ApplicantNeedsAssistanceCallingForHelpIndicator { get; set; }

        public string ApplicantNumberOfInstancesOfAlcoholConsumptionInPastOneYearCode { get; set; }

        public bool? ApplicantPhysicalDisabilityIndicator { get; set; }

        public bool? ApplicantRequiresAssistanceMeetingPersonalNeedsIndicator { get; set; }

        public bool? ApplicantRequiresLicensedNurseSupervisionIndicator { get; set; }

        public string ApplicantScreeningObservations { get; set; }

        public bool? ApplicantSubstanceAbuseIndicator { get; set; }

        public string ApplicantThinksWonderfulToBeAliveNowIndicator { get; set; }

        public string ApplicationStatusCode { get; set; }

        public string AssociationDateRangeEndDate { get; set; }

        public string AssociationDateRangeStartDate { get; set; }

        public string AssociationDescriptionText { get; set; }

        public string CareRecipientAssociationDateRangeEndDate { get; set; }

        public string CareRecipientAssociationDateRangeStartDate { get; set; }

        public string CareRecipientAssociationDescriptionText { get; set; }

        public string CareRecipientFamilyRelationshipCode { get; set; }

        public bool? CareRecipientHasCognitiveImpairment { get; set; }

        public bool? CareRecipientHasOtherDisabilities { get; set; }

        public string CareRecipientLivesWithCaregiver { get; set; }

        public string CareRecipientLocationCountyCode { get; set; }

        public string CareRecipientLocationStateUSPostalServiceCode { get; set; }

        public string CareRecipientPersonCaretakerDependentCode { get; set; }

        public string Comments { get; set; }

        public DateTime? CreateDateTime { get; set; }

        public string CreatedBy { get; set; }

        public string CurrentInterestListPersonCsilID { get; set; }

        public string DataEnteredBy { get; set; }

        public virtual ICollection<Enrollment> Enrollment { get; set; }

        public DateTime? EnteredDateTime { get; set; }

        public string FamilyRelationshipCode { get; set; }

        public string HasNoFixedAddressIndicator { get; set; }

        public string LivesOutsideStateTemporarilyIndicator { get; set; }

        public DateTime? MedicaidInformationDateTime { get; set; }

        public DateTime? MedicaidInformationDateTime1 { get; set; }

        public string PersonActiveMilitaryIndicator { get; set; }

        public string PersonCaretakerDependentCode { get; set; }

        public string PersonCmbhsID { get; set; }

        public string PersonCsilID { get; set; }

        public string PersonLtssID { get; set; }

        public bool? PersonMarriedIndicator { get; set; }

        public string PersonMedicaidID { get; set; }

        public string PersonMedicareID { get; set; }

        public string PersonPreferredLanguage { get; set; }

        public string PersonTiersID { get; set; }

        public bool? PersonUSVeteranIndicator { get; set; }

        public string PersonVeteranMilitaryIndicator { get; set; }

        public virtual ICollection<Referral> Referral { get; set; }

        public string ReferringOrganization { get; set; }

        public string ScreeningApplicantComments { get; set; }

        [Key]
        public string ScreeningID { get; set; }

        public string ScreeningQuestionnaireVersionIndicator { get; set; }

        public DateTime? TermsOfUseExpiryDate { get; set; }

        public string TermsOfUseExternalID { get; set; }

        public string TermsOfUseOrganizationID { get; set; }

        public DateTime? TermsOfUseSignatureDate { get; set; }

        public string TermsOfUseSignedBy { get; set; }

        public string TermsOfUseStaffName { get; set; }

        public string TermsOfUseTypeCode { get; set; }

        public string TermsOfUseVersionIndicator { get; set; }

        public string TypeOfMedicalInsurance { get; set; }

        public DateTime? UpdateDateTime { get; set; }

        public string UpdatedBy { get; set; }

        public bool? USNaturalizedCitizenIndicator { get; set; }
    }

    public class ScreeningConfiguration : EntityTypeConfiguration<Screening>
    {
        public ScreeningConfiguration()
        {
            HasMany(e => e.Referral)
            .WithRequired(e => e.Screening);

            HasMany(e => e.Enrollment)
            .WithRequired(e => e.Screening);
        }
    }
}