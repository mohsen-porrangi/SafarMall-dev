namespace WalletApp.API.Models.BankAccountDTOs
{
    public class CreateBankAccountDTO
    {
        public record AddBankAccountRequest(
      string BankName,
      string AccountNumber,
      string? CardNumber = null,
      string? ShabaNumber = null,
      string? AccountHolderName = null
  );
    }
}
