using GymManagement.BLL.Services.Interfaces;
using GymManagement.BLL.ViewModels.MemberViewModels;
using GymManagement.DAL.Models;
using GymManagement.DAL.Models.Enums;
using GymManagement.DAL.Repositories.Classes;
using GymManagement.DAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.BLL.Services.Classes
{
    public class MemberService : IMemberService
    {
        private readonly IUnitOfWork _unitOfWork;
        public MemberService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> CreateMemberAsync(CreateMemberViewModel model, CancellationToken ct = default)
        {
            //Check Email
            var emailExisit = await _unitOfWork.GetRepository<Member>().AnyAsync(m => m.Email == model.Email);
            //Check Phone
            var phoneExisit = await _unitOfWork.GetRepository<Member>().AnyAsync(m => m.Phone == model.Phone);


            if (emailExisit || phoneExisit) return false;



            var member = new Member()
            {
                Name = model.Name,
                Email = model.Email,
                Phone = model.Phone,
                Gender = model.Gender,
                DateOfBirth = model.DateOfBirth,
                Address = new Address()
                {
                    City = model.City,
                    Street = model.Street,
                    BuildingNumber = model.BuildingNumber
                },
                HealthRecord = new HealthRecord()
                {
                    Height = model.HealthRecordViewModel.Height,
                    Weight = model.HealthRecordViewModel.Weight,
                    BloodType = model.HealthRecordViewModel.BloodType,
                    Note = model.HealthRecordViewModel.Note,
                }
            };

            _unitOfWork.GetRepository<Member>().AddAsync(member);
            var result = await _unitOfWork.SaveChangesAsync();

            return result > 0;

        }

        public async Task<IEnumerable<MemberViewModel>> GetAllAsync(CancellationToken ct = default)
        {
            var members = await _unitOfWork.GetRepository<Member>().GetAllAsync(ct: ct);
            if (!members.Any()) return Enumerable.Empty<MemberViewModel>();

            return members.Select(member=> new MemberViewModel()
            {
                Name = member.Name,
                Email = member.Email,
                Gender = member.Gender.ToString(),
                Id = member.Id,
                Phone = member.Phone,
                Photo = member.Photo

            });   
        }

         

        public async Task<MemberViewModel?> GetMemberDetailsByIdAsync(int memberId, CancellationToken ct = default)
        {
            var member = await _unitOfWork.GetRepository<Member>().GetByIdAsync(memberId, ct);
            if (member is null) return null;

            var model = new MemberViewModel()
            {
                Name = member.Name,
                Phone = member.Phone,
                DateOfBirth = member.DateOfBirth.ToString(),
                Gender = member.Gender.ToString(),
                Address = $"{member.Address.BuildingNumber} -  {member.Address.Street} - {member.Address.City}",
                Email = member.Email
            };

            var activeMembership = await _unitOfWork.GetRepository<Membership>().FirstOrDefaultAsync(m => m.MemberId == memberId && m.EndDate > DateTime.Now);
            
            if (activeMembership is not null)
            {
                var activePlan = await _unitOfWork.GetRepository<Plan>().GetByIdAsync(activeMembership.PlanId, ct);

                model.PlanName = activePlan?.Name;
                model.MembershipStartDate = activeMembership.CreatedAt.ToString();
                model.MembershipEndDate = activeMembership.EndDate.ToString();

            }

            return model;

        }

        public async Task<HealthRecordViewModel?> GetMemberHealthRecordViewModel(int memberId, CancellationToken ct = default)
        {
            var record = await _unitOfWork.GetRepository<HealthRecord>().FirstOrDefaultAsync(h => h.MemberId == memberId, ct: ct);
            if (record is null) return null;
            else return new HealthRecordViewModel()
            {
                Height = record.Height,
                Weight = record.Weight,
                BloodType = record.BloodType,
                Note = record.Note
            };
        }

        public async Task<MemberToUpdateViewModel?> GetMemberToUpdateAsync(int memberId, CancellationToken ct = default)
        {
            var member = await _unitOfWork.GetRepository<Member>().GetByIdAsync(memberId, ct: ct);
            if (member is null) return null;
            else return new MemberToUpdateViewModel()
            {
                Name = member.Name,
                Phone = member.Phone,
                Email = member.Email,
                BuildingNumber = member.Address.BuildingNumber,
                City = member.Address.City,
                Street = member.Address.Street,
                Photo = member.Photo
            };
        }

        public async Task<bool> DeleteMemberAsync(int memberId, CancellationToken ct)
        {
            var member = await _unitOfWork.GetRepository<Member>().GetByIdAsync(memberId);
            if (member is null) return false;

            //Cannot Remove Member With Future Booking

            var hasFutureBooking = await _unitOfWork.GetRepository<Booking>().AnyAsync(b => b.MemberId == memberId &&
                                                                          b.Session.StartDate > DateTime.Now);

            if (hasFutureBooking) return false;

            _unitOfWork.GetRepository<Member>().DeleteAsync(member);
            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> UpdateMemberAsync(int id, MemberToUpdateViewModel model, CancellationToken ct)
        {

            var member = await _unitOfWork.GetRepository<Member>().GetByIdAsync(id);
            if (member is null) return false;

            var emailExisits = await _unitOfWork.GetRepository<Member>().AnyAsync(m => m.Email == model.Email && m.Id != id);
            var phoneExisits = await _unitOfWork.GetRepository<Member>().AnyAsync(m => m.Phone == model.Phone && m.Id != id);

            if (emailExisits || phoneExisits) return false;

            member.Email = model.Email;
            member.Phone = model.Phone;
            member.Address.City = model.City;
            member.Address.Street = model.Street;
            member.Address.BuildingNumber = model.BuildingNumber;
            member.UpdatedAt = DateTime.Now;

             _unitOfWork.GetRepository<Member>().UpdateAsync(member, ct) ;
            var result = await _unitOfWork.SaveChangesAsync();
            return result > 0;
        }
    }
}
