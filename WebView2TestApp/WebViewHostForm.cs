namespace WebView2TestApp
{
    public partial class WebViewHostForm : Form
    {
        public WebViewHostForm()
        {
            InitializeComponent();
            this.Load += WebViewHostForm_Load;
        }

        private async void WebViewHostForm_Load(object sender, EventArgs e)
        {
            try
            {
                // Ensure WebView2 is initialized
                await webView21.EnsureCoreWebView2Async(null);

                // Load the HTML login form
                string htmlContent = GetLoginFormHtml();
                webView21.CoreWebView2.NavigateToString(htmlContent);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error initializing WebView2: {0}", ex.Message), 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetLoginFormHtml()
        {
            return @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Login Form</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            margin: 0;
        }
        
        .login-container {
            background: white;
            border-radius: 8px;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
            padding: 30px;
            width: 100%;
            max-width: 350px;
        }
        
        .login-header {
            text-align: center;
            margin-bottom: 25px;
        }
        
        .login-header h1 {
            color: #333;
            font-size: 24px;
            margin: 0 0 10px 0;
        }
        
        .login-header p {
            color: #666;
            font-size: 14px;
            margin: 0;
        }
        
        .form-group {
            margin-bottom: 18px;
        }
        
        .form-group label {
            display: block;
            color: #333;
            font-weight: bold;
            margin-bottom: 6px;
            font-size: 14px;
        }
        
        .form-group input {
            width: 100%;
            padding: 10px;
            border: 2px solid #ddd;
            border-radius: 4px;
            font-size: 14px;
            box-sizing: border-box;
        }
        
        .form-group input:focus {
            outline: none;
            border-color: #667eea;
        }
        
        .login-button {
            width: 100%;
            padding: 12px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            border: none;
            border-radius: 4px;
            font-size: 16px;
            font-weight: bold;
            cursor: pointer;
            margin-top: 10px;
        }
        
        .login-button:hover {
            opacity: 0.9;
        }
        
        .message {
            margin-top: 15px;
            padding: 10px;
            border-radius: 4px;
            text-align: center;
            display: none;
            font-size: 14px;
        }
        
        .message.success {
            background-color: #d4edda;
            color: #155724;
            border: 1px solid #c3e6cb;
        }
        
        .message.error {
            background-color: #f8d7da;
            color: #721c24;
            border: 1px solid #f5c6cb;
        }
    </style>
</head>
<body>
    <div class=""login-container"">
        <div class=""login-header"">
            <h1>Welcome</h1>
            <p>Please login to continue</p>
        </div>
        
        <form id=""loginForm"" onsubmit=""handleLogin(event)"">
            <div class=""form-group"">
                <label for=""username"">Username</label>
                <input type=""text"" id=""username"" name=""username"" placeholder=""Enter username"" required>
            </div>
            
            <div class=""form-group"">
                <label for=""password"">Password</label>
                <input type=""password"" id=""password"" name=""password"" placeholder=""Enter password"" required>
            </div>
            
            <button type=""submit"" class=""login-button"">Login</button>
            
            <div id=""message"" class=""message""></div>
        </form>
    </div>
    
    <script>
        function handleLogin(event) {
            event.preventDefault();
            
            var username = document.getElementById('username').value;
            var password = document.getElementById('password').value;
            var messageDiv = document.getElementById('message');
            
            if (username && password) {
                messageDiv.className = 'message success';
                messageDiv.textContent = 'Login successful! Username: ' + username;
                messageDiv.style.display = 'block';
            } else {
                messageDiv.className = 'message error';
                messageDiv.textContent = 'Please fill in all fields';
                messageDiv.style.display = 'block';
            }
            
            setTimeout(function() {
                messageDiv.style.display = 'none';
            }, 3000);
        }
    </script>
</body>
</html>";
        }
    }
}
