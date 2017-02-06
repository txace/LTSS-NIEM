declare @referralid int;
set @referralid = '999999';

select * into #ReferralContact from
(select r.ReferralID, c.* from referral r
join Screening s on r.ScreeningID = s.ScreeningID
join EntityContactEntity ec on s.ScreeningID = ec.EntityID
join ContactEntity c on ec.ContactEntityID = c.ContactEntityID
where ec.ContactEntityType in ('CI', 'SA', 'PA', 'CR') AND r.ReferralID = @referralid) a;

select r.ReferralID,
     r.ReferralReason,
     c.PersonFullName,
     c.PersonSSNIdentification,
     c.PersonBirthDate,   
     c.ContactTelephoneNumber,
     c.SEXCode,
     c.RACCode,
     c.ContactMailingAddress,   
     e.ProgramName, e.PlanName, e.StartDate as EnrollmentDate, s.*, l.LocationName
     from (select
           (SELECT TOP(1) ReferralID FROM #ReferralContact) AS ReferralID,
           (SELECT TOP(1) PersonFullName FROM #ReferralContact WHERE PersonFullName IS NOT NULL) AS PersonFullName,
           (SELECT TOP(1) PersonSSNIdentification FROM #ReferralContact WHERE PersonSSNIdentification IS NOT NULL) AS PersonSSNIdentification,
           (SELECT TOP(1) PersonBirthDate FROM #ReferralContact WHERE PersonBirthDate IS NOT NULL) AS PersonBirthDate,
           (SELECT TOP(1) ContactTelephoneNumber FROM #ReferralContact WHERE ContactTelephoneNumber IS NOT NULL) AS ContactTelephoneNumber,
           (SELECT TOP(1) SEXCode FROM #ReferralContact WHERE SEXCode IS NOT NULL) AS SEXCode,
           (SELECT TOP(1) RACCode FROM #ReferralContact WHERE RACCode IS NOT NULL) AS RACCode,
           (SELECT TOP(1) ContactMailingAddress FROM #ReferralContact WHERE ContactMailingAddress IS NOT NULL) AS ContactMailingAddress) c
     join referral r on c.ReferralID = r.ReferralID
     join Organization o on r.DestinationOrganizationID = o.OrganizationID
     join Location l on o.OrganizationID = l.OrganizationID
     join Screening s on r.ScreeningID = s.ScreeningID
     join Enrollment e on s.ScreeningID = e.ScreeningID   
     where r.ReferralID = @referralid;    

drop table #ReferralContact;