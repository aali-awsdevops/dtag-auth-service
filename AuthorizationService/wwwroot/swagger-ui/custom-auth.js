(function () {
  // Robust Swagger login helper. Tries multiple insertion points and falls back to a floating widget.
  console.info('custom-auth.js loaded');

  async function setSwaggerToken(token) {
    // Wait briefly for swagger UI instance
    var ui = await new Promise(function (resolve) {
      if (window.ui && (window.ui.authActions || window.ui.preauthorizeApiKey)) return resolve(window.ui);
      var i = setInterval(function () { if (window.ui && (window.ui.authActions || window.ui.preauthorizeApiKey)) { clearInterval(i); resolve(window.ui); } }, 100);
      // timeout after 3s
      setTimeout(function () { clearInterval(i); resolve(window.ui); }, 3000);
    });

    try {
      if (ui && ui.authActions && ui.authActions.authorize) {
        // Ensure any previous auth state is cleared to avoid duplicate 'Bearer ' prefixes
        try { if (ui.authActions.logout) ui.authActions.logout(); } catch (e) { /* ignore */ }

        // Some swagger builds expect the raw token (they add the "Bearer " prefix themselves),
        // so set the token value only. If your swagger instance requires the full "Bearer <token>",
        // change this to 'Bearer ' + token.
        var authObj = { Bearer: { name: 'Authorization', schema: { type: 'http', scheme: 'bearer', bearerFormat: 'JWT' }, value: token } };
        ui.authActions.authorize(authObj);
        console.info('Token set via ui.authActions.authorize');
        return true;
      }
      if (ui && ui.preauthorizeApiKey) {
        // Some older swagger-ui versions expect the API key value directly. Clear any previous setting first.
        try { if (ui.authActions && ui.authActions.logout) ui.authActions.logout(); } catch (e) { }
        // Same reasoning as above: pass raw token. If your Swagger UI requires the scheme prefix, use 'Bearer ' + token.
        ui.preauthorizeApiKey('Bearer', token);
        console.info('Token set via ui.preauthorizeApiKey');
        return true;
      }
    } catch (e) {
      console.warn('Failed to set token via swagger-ui API', e);
    }

    // As a last resort, set default Authorization header for fetch used by swagger-ui (may not be used)
    try { window.__swagger_token = token; } catch { }
    return false;
  }

  async function loginAndSet(email, password, statusEl) {
    statusEl.textContent = '';
    try {
      var res = await fetch('/Auth/token', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: email, password: password })
      });
      if (!res.ok) {
        statusEl.style.color = 'red';
        statusEl.textContent = 'Login failed: ' + res.status;
        console.warn('Login failed', res.status);
        return;
      }
      var data = await res.json();
      var token = data.token || data.Token;
      if (!token) { statusEl.style.color='red'; statusEl.textContent = 'No token in response'; console.warn('No token in response', data); return; }
      await setSwaggerToken(token);
      statusEl.style.color = 'green';
      statusEl.textContent = 'Token set';
    } catch (e) {
      console.error(e);
      statusEl.style.color = 'red';
      statusEl.textContent = 'Error';
    }
  }

  function buildWidget() {
    var wrapper = document.createElement('div');
    wrapper.id = 'swagger-login-widget';
    wrapper.style.position = 'fixed';
    wrapper.style.top = '10px';
    wrapper.style.right = '10px';
    wrapper.style.zIndex = '2000';
    wrapper.style.background = 'white';
    wrapper.style.border = '1px solid #ddd';
    wrapper.style.padding = '8px';
    wrapper.style.borderRadius = '6px';
    wrapper.style.boxShadow = '0 2px 8px rgba(0,0,0,0.1)';
    wrapper.style.display = 'flex';
    wrapper.style.gap = '6px';
    wrapper.style.alignItems = 'center';

    var email = document.createElement('input');
    email.placeholder = 'Email';
    email.id = 'swagger_email';
    email.style.padding = '6px';
    email.style.border = '1px solid #ccc';
    email.style.borderRadius = '4px';
    email.style.width = '160px';

    var pass = document.createElement('input');
    pass.placeholder = 'Password';
    pass.type = 'password';
    pass.id = 'swagger_password';
    pass.style.padding = '6px';
    pass.style.border = '1px solid #ccc';
    pass.style.borderRadius = '4px';
    pass.style.width = '120px';

    var btn = document.createElement('button');
    btn.textContent = 'Login';
    btn.style.padding = '6px 10px';
    btn.style.cursor = 'pointer';

    var status = document.createElement('span');
    status.id = 'swagger_msg';
    status.style.marginLeft = '6px';
    status.style.fontSize = '12px';

    btn.addEventListener('click', function () { loginAndSet(email.value, pass.value, status); });

    var close = document.createElement('button');
    close.textContent = '×';
    close.title = 'Close';
    close.style.marginLeft = '6px';
    close.style.border = 'none';
    close.style.background = 'transparent';
    close.style.cursor = 'pointer';
    close.addEventListener('click', function () { wrapper.remove(); });

    wrapper.appendChild(email);
    wrapper.appendChild(pass);
    wrapper.appendChild(btn);
    wrapper.appendChild(status);
    wrapper.appendChild(close);

    return wrapper;
  }

  function tryInsertTopbar(widget) {
    var selectors = ['.swagger-ui .topbar', '.swagger-ui .topbar .topbar-wrapper', '.topbar', '#swagger-ui', '.swagger-ui'];
    for (var s of selectors) {
      var el = document.querySelector(s);
      if (el) {
        // try to append into existing right area
        var right = el.querySelector('.download-url-wrapper') || el.querySelector('.topbar-wrapper') || el;
        right.appendChild(widget);
        return true;
      }
    }
    return false;
  }

  function init() {
    var widget = buildWidget();
    var inserted = tryInsertTopbar(widget);
    if (!inserted) {
      // fallback: floating widget
      document.body.appendChild(widget);
    }
  }

  window.addEventListener('load', function () { setTimeout(init, 400); });
})();
