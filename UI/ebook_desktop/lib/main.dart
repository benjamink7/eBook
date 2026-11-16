import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'core/auth_provider.dart';
import 'core/navigation.dart';
import 'screens/login_screen.dart';
import 'screens/shell_screen.dart';

void main() {
  runApp(
    ChangeNotifierProvider(create: (_) => AuthProvider(), child: const EBookAdminApp()),
  );
}

class EBookAdminApp extends StatelessWidget {
  const EBookAdminApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'eBook Admin',
      debugShowCheckedModeBanner: false,
      scaffoldMessengerKey: messengerKey,
      theme: ThemeData(colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF3F51B5)), useMaterial3: true),
      home: Consumer<AuthProvider>(
        builder: (context, auth, _) => auth.isAuthenticated ? const ShellScreen() : const LoginScreen(),
      ),
    );
  }
}
