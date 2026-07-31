// Generic Firestore access for cloud sync.
//
// Layout: users/{uid}/{collection}/{documentId}, with three primitive fields per
// document: UpdatedAt (ISO-8601 string), IsDeleted (bool) and Payload (the record
// as JSON). Exactly the same shape the Android and iOS build writes, so a phone
// and a browser read each other's data.
//
// Nothing in here catches errors on purpose: a failure has to reach C# so the app
// can show "sync failed" instead of quietly doing nothing.

window.firebaseSync = {

    // Firestore rejects batches larger than 500 operations.
    batchLimit: 400,

    collectionRef: function (userId, collection) {
        return firebase.firestore()
            .collection('users')
            .doc(userId)
            .collection(collection);
    },

    toSyncDocument: function (id, data) {
        if (!data) {
            return null;
        }

        var updatedAt = data.UpdatedAt;

        // Tolerate documents written as a Firestore Timestamp by an older build.
        if (updatedAt && typeof updatedAt.toDate === 'function') {
            updatedAt = updatedAt.toDate().toISOString();
        }

        return {
            Id: id,
            UpdatedAt: updatedAt || '',
            IsDeleted: data.IsDeleted === true,
            Payload: data.Payload || ''
        };
    },

    getAll: async function (userId, collection) {
        const snapshot = await window.firebaseSync.collectionRef(userId, collection).get();

        const documents = [];
        snapshot.forEach(doc => {
            const mapped = window.firebaseSync.toSyncDocument(doc.id, doc.data());
            if (mapped) {
                documents.push(mapped);
            }
        });

        return JSON.stringify(documents);
    },

    get: async function (userId, collection, id) {
        const doc = await window.firebaseSync.collectionRef(userId, collection).doc(id).get();

        if (!doc.exists) {
            return null;
        }

        const mapped = window.firebaseSync.toSyncDocument(doc.id, doc.data());
        return mapped ? JSON.stringify(mapped) : null;
    },

    upsert: async function (userId, collection, documentsJson) {
        const documents = JSON.parse(documentsJson);
        if (!documents || documents.length === 0) {
            return;
        }

        const reference = window.firebaseSync.collectionRef(userId, collection);

        for (let offset = 0; offset < documents.length; offset += window.firebaseSync.batchLimit) {
            const slice = documents.slice(offset, offset + window.firebaseSync.batchLimit);
            const batch = firebase.firestore().batch();

            for (const document of slice) {
                if (!document.Id) {
                    continue;
                }

                batch.set(reference.doc(document.Id), {
                    UpdatedAt: document.UpdatedAt || '',
                    IsDeleted: document.IsDeleted === true,
                    Payload: document.Payload || ''
                });
            }

            await batch.commit();
        }
    },

    hardDelete: async function (userId, collection, idsJson) {
        const ids = JSON.parse(idsJson);
        if (!ids || ids.length === 0) {
            return;
        }

        const reference = window.firebaseSync.collectionRef(userId, collection);

        for (let offset = 0; offset < ids.length; offset += window.firebaseSync.batchLimit) {
            const slice = ids.slice(offset, offset + window.firebaseSync.batchLimit);
            const batch = firebase.firestore().batch();

            for (const id of slice) {
                if (id) {
                    batch.delete(reference.doc(id));
                }
            }

            await batch.commit();
        }
    }
};
