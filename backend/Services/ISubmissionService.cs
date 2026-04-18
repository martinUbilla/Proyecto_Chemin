using backend.Dtos;

namespace backend.Services
{
    public interface ISubmissionService
    {
        Task<Boolean> SubmitDocuments(LoginRequest request);
    }
}
