/*using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class UserRepository
{
    public UserRepository()
    {
    }
    
    public async UniTask<User> GetUserByWalletIdAsync(string walletId)
    {
        try
        {
            Debug.Log("Вход в метод GetUserByWalletIdAsync.");
			
            
        
            Debug.Log("Запрос к базе данных выполнен.");
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при получении пользователя: {ex.Message}");
            Debug.LogError($"Stack Trace: {ex.StackTrace}");
            return null;
        }
    }
    
    public async UniTask UpdateUserAsync(User user)
    {
        //await _supabaseClient.From<User>().Update(user);
    }
}*/