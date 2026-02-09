namespace PaymentGateway.Application.Encryption;

public interface IEncryptionService
{
   public  string Encrypt(string plaintext);
   public  string Decrypt(string ciphertext);
}