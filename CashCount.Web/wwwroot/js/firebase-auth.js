// Firebase Authentication and Firestore JavaScript wrapper for Blazor interop

window.firebaseAuth = {
    dotNetRef: null,

    // Initialize Firebase Auth and set up auth state listener
    initialize: function (dotNetRef) {
        this.dotNetRef = dotNetRef;

        // Listen for auth state changes
        firebase.auth().onAuthStateChanged((user) => {
            if (this.dotNetRef) {
                const userProfile = user ? this.mapUser(user) : null;
                this.dotNetRef.invokeMethodAsync('OnAuthStateChanged', userProfile);
            }
        });
    },

    // Sign in with email and password
    signInWithEmail: async function (email, password) {
        const userCredential = await firebase.auth()
            .signInWithEmailAndPassword(email, password);
        return this.mapUser(userCredential.user);
    },

    // Sign up with email and password
    signUpWithEmail: async function (email, password, displayName) {
        const userCredential = await firebase.auth()
            .createUserWithEmailAndPassword(email, password);

        // Update display name
        if (displayName) {
            await userCredential.user.updateProfile({ displayName: displayName });
        }

        return this.mapUser(userCredential.user);
    },

    // Sign in with Google
    signInWithGoogle: async function () {
        const provider = new firebase.auth.GoogleAuthProvider();
        const userCredential = await firebase.auth().signInWithPopup(provider);
        return this.mapUser(userCredential.user);
    },

    // Sign in with Apple
    signInWithApple: async function () {
        const provider = new firebase.auth.OAuthProvider('apple.com');
        provider.addScope('email');
        provider.addScope('name');
        const userCredential = await firebase.auth().signInWithPopup(provider);
        return this.mapUser(userCredential.user);
    },

    // Sign in with Microsoft
    signInWithMicrosoft: async function () {
        const provider = new firebase.auth.OAuthProvider('microsoft.com');
        const userCredential = await firebase.auth().signInWithPopup(provider);
        return this.mapUser(userCredential.user);
    },

    // Sign out
    signOut: async function () {
        await firebase.auth().signOut();
    },

    // Get current user
    getCurrentUser: function () {
        const user = firebase.auth().currentUser;
        return user ? this.mapUser(user) : null;
    },

    // Send password reset email
    sendPasswordResetEmail: async function (email) {
        await firebase.auth().sendPasswordResetEmail(email);
    },

    // Update display name
    updateDisplayName: async function (displayName) {
        const user = firebase.auth().currentUser;
        if (user) {
            await user.updateProfile({ displayName: displayName });
        }
    },

    // Map Firebase user to our profile object
    mapUser: function (user) {
        if (!user) return null;

        // Determine auth provider
        let authProvider = 'email';
        if (user.providerData && user.providerData.length > 0) {
            authProvider = user.providerData[0].providerId;
        }

        return {
            userId: user.uid,
            email: user.email,
            displayName: user.displayName,
            photoUrl: user.photoURL,
            authProvider: authProvider
        };
    }
};

// Firestore operations
window.firebaseFirestore = {
    // Get user profile from Firestore
    getUserProfile: async function (userId) {
        try {
            const doc = await firebase.firestore()
                .collection('users')
                .doc(userId)
                .get();

            if (!doc.exists) return null;

            const data = doc.data();
            // Convert Firestore timestamps to ISO strings
            if (data.CreatedAt && data.CreatedAt.toDate) {
                data.CreatedAt = data.CreatedAt.toDate().toISOString();
            }
            if (data.LastLoginAt && data.LastLoginAt.toDate) {
                data.LastLoginAt = data.LastLoginAt.toDate().toISOString();
            }
            if (data.PremiumExpiryDate && data.PremiumExpiryDate.toDate) {
                data.PremiumExpiryDate = data.PremiumExpiryDate.toDate().toISOString();
            }

            return JSON.stringify(data);
        } catch (error) {
            console.error('getUserProfile error:', error);
            return null;
        }
    },

    // Save user profile to Firestore
    saveUserProfile: async function (userId, profileJson) {
        try {
            const profile = JSON.parse(profileJson);

            // Convert date strings to Firestore timestamps
            if (profile.CreatedAt) {
                profile.CreatedAt = firebase.firestore.Timestamp.fromDate(new Date(profile.CreatedAt));
            }
            if (profile.LastLoginAt) {
                profile.LastLoginAt = firebase.firestore.Timestamp.fromDate(new Date(profile.LastLoginAt));
            }
            if (profile.PremiumExpiryDate) {
                profile.PremiumExpiryDate = firebase.firestore.Timestamp.fromDate(new Date(profile.PremiumExpiryDate));
            }

            await firebase.firestore()
                .collection('users')
                .doc(userId)
                .set(profile, { merge: true });
        } catch (error) {
            console.error('saveUserProfile error:', error);
        }
    },

    // Update premium status
    updatePremiumStatus: async function (userId, isPremium, expiryMs) {
        try {
            const updates = {
                IsPremium: isPremium
            };

            if (expiryMs) {
                updates.PremiumExpiryDate = firebase.firestore.Timestamp.fromMillis(expiryMs);
            }

            await firebase.firestore()
                .collection('users')
                .doc(userId)
                .update(updates);
        } catch (error) {
            console.error('updatePremiumStatus error:', error);
        }
    },

    // Sync saved counts to Firestore
    syncSavedCounts: async function (userId, countsJson) {
        try {
            const counts = JSON.parse(countsJson);
            const batch = firebase.firestore().batch();
            const countsRef = firebase.firestore()
                .collection('users')
                .doc(userId)
                .collection('savedCounts');

            for (const count of counts) {
                const docRef = countsRef.doc(count.Id);
                batch.set(docRef, count);
            }

            await batch.commit();
        } catch (error) {
            console.error('syncSavedCounts error:', error);
        }
    },

    // Get synced counts from Firestore
    getSyncedCounts: async function (userId) {
        try {
            const snapshot = await firebase.firestore()
                .collection('users')
                .doc(userId)
                .collection('savedCounts')
                .get();

            const counts = [];
            snapshot.forEach(doc => {
                const data = doc.data();
                // Convert timestamps if needed
                if (data.SavedAt && data.SavedAt.toDate) {
                    data.SavedAt = data.SavedAt.toDate().toISOString();
                }
                counts.push(data);
            });

            return JSON.stringify(counts);
        } catch (error) {
            console.error('getSyncedCounts error:', error);
            return '[]';
        }
    },

    // Delete user data
    deleteUserData: async function (userId) {
        try {
            // Delete saved counts subcollection
            const countsSnapshot = await firebase.firestore()
                .collection('users')
                .doc(userId)
                .collection('savedCounts')
                .get();

            const batch = firebase.firestore().batch();

            countsSnapshot.forEach(doc => {
                batch.delete(doc.ref);
            });

            // Delete user document
            batch.delete(firebase.firestore().collection('users').doc(userId));

            await batch.commit();
        } catch (error) {
            console.error('deleteUserData error:', error);
        }
    }
};
