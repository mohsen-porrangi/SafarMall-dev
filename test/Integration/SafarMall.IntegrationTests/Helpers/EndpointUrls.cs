using SafarMall.IntegrationTests.Configuration;

namespace SafarMall.IntegrationTests.Helpers;

public static class EndpointUrls
{
    // Base URLs
    public static string UserManagementBase => TestConfiguration.ServiceUrls.UserManagement;
    public static string WalletBase => TestConfiguration.ServiceUrls.Wallet;
    public static string OrderBase => TestConfiguration.ServiceUrls.Order;
    public static string PaymentGatewayBase => TestConfiguration.ServiceUrls.PaymentGateway;

    // User Management Endpoints
    public static class UserManagement
    {
        public static string Register => $"{UserManagementBase}/api/auth/register";
        public static string VerifyOtp => $"{UserManagementBase}/api/auth/register/verify-otp";
        public static string Login => $"{UserManagementBase}/api/auth/login";
        public static string CurrentUser => $"{UserManagementBase}/api/users/current";
        public static string UpdateProfile => $"{UserManagementBase}/api/users/current";
        public static string ResetPassword => $"{UserManagementBase}/api/auth/reset-password";
        public static string RefreshToken => $"{UserManagementBase}/api/auth/refresh-token";

        // Admin endpoints
        public static string SearchUsers => $"{UserManagementBase}/api/users/search";
        public static string ChangeUserStatus(Guid userId) => $"{UserManagementBase}/api/users/{userId}/status";
        public static string DeleteUser(Guid userId) => $"{UserManagementBase}/api/users/{userId}";

        // Role Management
        public static string GetRoles => $"{UserManagementBase}/api/roles";
        public static string CreateRole => $"{UserManagementBase}/api/roles";
        public static string UpdateRole(int roleId) => $"{UserManagementBase}/api/roles/{roleId}";
        public static string DeleteRole(int roleId) => $"{UserManagementBase}/api/roles/{roleId}";
        public static string GetUserRoles(Guid userId) => $"{UserManagementBase}/api/users/{userId}/roles";
        public static string AssignRoleToUser(Guid userId) => $"{UserManagementBase}/api/users/{userId}/roles";
        public static string UnassignRoleFromUser(Guid userId, int roleId) => $"{UserManagementBase}/api/users/{userId}/roles/{roleId}";
    }

    // Wallet Payment Endpoints
    public static class Wallet
    {
        public static string CreateWallet => $"{WalletBase}/api/wallets";
        public static string GetWalletBalance => $"{WalletBase}/api/wallets/balance";
        public static string GetWalletSummary => $"{WalletBase}/api/wallets/summary";

        // Transactions
        public static string DirectDeposit => $"{WalletBase}/api/transactions/direct-deposit";
        public static string IntegratedPurchase => $"{WalletBase}/api/transactions/integrated-purchase";
        public static string WalletTransfer => $"{WalletBase}/api/transactions/transfer";
        public static string PaymentCallback => $"{WalletBase}/api/transactions/payment-callback";
        public static string TransactionHistory => $"{WalletBase}/api/transactions/history";
        public static string RefundTransaction(Guid transactionId) => $"{WalletBase}/api/transactions/{transactionId}/refund";
        public static string GetRefundableTransactions => $"{WalletBase}/api/transactions/refundable";

        // Bank Accounts
        public static string AddBankAccount => $"{WalletBase}/api/bank-accounts";
        public static string GetBankAccounts => $"{WalletBase}/api/bank-accounts";
        public static string RemoveBankAccount(Guid bankAccountId) => $"{WalletBase}/api/bank-accounts/{bankAccountId}";

        // Internal endpoints
        public static string CreateWalletInternal => $"{WalletBase}/api/internal/wallets/create";
        public static string CheckWalletExists(Guid userId) => $"{WalletBase}/api/internal/wallets/{userId}/exists";
        public static string GetWalletBalanceInternal(Guid userId) => $"{WalletBase}/api/internal/wallets/{userId}/balance";
        public static string CheckAffordability(Guid userId) => $"{WalletBase}/api/internal/wallets/{userId}/check-affordability";
    }

    // Order Endpoints
    public static class Order
    {
        public static string CreateOrder => $"{OrderBase}/api/orders";
        public static string GetOrder(Guid orderId) => $"{OrderBase}/api/orders/{orderId}";
        public static string GetOrderDetails(Guid orderId) => $"{OrderBase}/api/orders/{orderId}/details";
        public static string GetUserOrders => $"{OrderBase}/api/orders";
        public static string CancelOrder(Guid orderId) => $"{OrderBase}/api/orders/{orderId}/cancel";

        // Passengers
        public static string SavePassenger => $"{OrderBase}/api/passengers";
        public static string GetSavedPassengers => $"{OrderBase}/api/passengers";
        public static string DeleteSavedPassenger(long passengerId) => $"{OrderBase}/api/passengers/{passengerId}";

        // Tickets
        public static string GetFlightTickets(Guid orderId) => $"{OrderBase}/api/orders/{orderId}/flights";
        public static string GetTrainTickets(Guid orderId) => $"{OrderBase}/api/orders/{orderId}/trains";

        // Internal endpoints
        public static string CreateOrderInternal => $"{OrderBase}/api/internal/orders";
        public static string CompleteOrderInternal(Guid orderId) => $"{OrderBase}/api/internal/orders/{orderId}/complete";
        public static string UpdateOrderStatusInternal(Guid orderId) => $"{OrderBase}/api/internal/orders/{orderId}/status";
    }

    // Payment Gateway Endpoints
    public static class PaymentGateway
    {
        public static string CreatePayment => $"{PaymentGatewayBase}/api/payments";
        public static string VerifyPayment => $"{PaymentGatewayBase}/api/payments/verify";
        public static string PaymentCallback => $"{PaymentGatewayBase}/api/payments/callback";
        public static string GetPaymentStatus(string paymentId) => $"{PaymentGatewayBase}/api/payments/{paymentId}/status";
        public static string GetPaymentStatusByReference => $"{PaymentGatewayBase}/api/payments/status";

        // Webhooks
        public static string ZarinPalWebhook => $"{PaymentGatewayBase}/api/webhooks/zarinpal";
        public static string ZibalWebhook => $"{PaymentGatewayBase}/api/webhooks/zibal";
        public static string SandboxWebhook => $"{PaymentGatewayBase}/api/webhooks/sandbox";
    }
}