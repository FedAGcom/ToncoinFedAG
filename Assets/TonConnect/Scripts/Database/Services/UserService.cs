/*using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class UserService
{
    private readonly UserRepository _userRepository;
    private User _currentUser;

    public UserService(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async UniTask<string> RegisterOrAuthenticateUserAsync(User user)
    {
        try
        {
            Debug.Log("Попытка получения пользователя по WalletId.");
            var currentUser = await _userRepository.GetUserByWalletIdAsync(user.WalletId);
            Debug.Log("Проверка, получен ли пользователь.");
            
            if (currentUser != null)
            {
                // Authenticate the user
                _currentUser = currentUser;
                Debug.Log($"Аутентификация успешна! Добро пожаловать, {_currentUser.Name}.");
                return $"Authentication successful! Welcome back, {_currentUser.Name}.";
            }

            // Register the new user
            Debug.Log("Пользователь не найден, попытка регистрации нового пользователя.");
            /*var response = await _userRepository.InsertUserAsync(user);

            if (response.ResponseMessage.IsSuccessStatusCode)
            {
                Debug.Log("Регистрация успешна, попытка получения данных пользователя.");
                _currentUser = response.Models.FirstOrDefault();
                if (_currentUser != null)
                {
                    Debug.Log($"Регистрация успешна! Добро пожаловать, {_currentUser.Name}. WalletId: {_currentUser.WalletId}");
                    return $"Registration successful!\nWelcome, {_currentUser.Name}. WalletId: {_currentUser.WalletId}";
                }

                Debug.Log("Регистрация успешна, но не удалось получить данные пользователя.");
                return "Registration successful, but could not retrieve user details.";
            }#1#

            Debug.Log("Регистрация не удалась: нет ответа или пустой ответ.");
            return "Registration failed: No response or empty response.";
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка во время регистрации или аутентификации: {ex.Message}");
            Debug.LogError($"Stack Trace: {ex.StackTrace}");
            return $"Registration or authentication failed: {ex.Message}";
        }
    }

    public User GetCurrentUser()
    {
        return _currentUser;
    }
    
    public async UniTask UpdateUserAsync(User user)
    {
        await _userRepository.UpdateUserAsync(user);
    }
}*/