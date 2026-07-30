using AutoMapper;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TourAPI.DTOs;
using TourAPI.Models;

namespace TourAPI.Mappers
{
    public class ReviewReplyProfile : Profile
    {
       public ReviewReplyProfile() {
            CreateMap<ReviewReply, ReadReviewReplyDTO>(); 
            CreateMap<CreateReviewReplyDTO, ReviewReply>();
            CreateMap<UpdateReviewReplyDTO, ReviewReply>();
        }
    }
}
