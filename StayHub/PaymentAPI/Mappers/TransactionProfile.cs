using AutoMapper;
using PaymentAPI.DTOs;
using PaymentAPI.Models;

namespace PaymentAPI.Mappers
{
    public class TransactionProfile : Profile
    {
        public TransactionProfile()
        {
            CreateMap<Transaction, ReadTransactionDTO>();
            CreateMap<CreateTransactionDTO, Transaction>();
            CreateMap<UpdateTransactionDTO, Transaction>();
        }
    }
}
