<template>
    <div>
        <h1 v-if="username">Welcome, {{ username }}!</h1>
        <p v-else>Fetching user info...</p>  <!-- Fallback text if username is empty -->

        <div v-if="playlist != null && playlist.songs.length > 0" class="recent-tracks">
            <h2>Playlist tracks: {{ playlist.name }}</h2>
            <div class="progress-container">
                <h4>Playlist duration: {{ currentSelectedSongsDurationInMinutes }} minutes / {{ neededDurationForSessionInMinutes }} minutes needed</h4>
                <progress :value="currentSelectedSongsDurationInMinutes" :max="neededDurationForSessionInMinutes"></progress>
            </div>
            <div class="track-grid">
            <div
                v-for="(track, index) in playlist.songs"
                :key="index"
                :class="{ 'selected-song': selectedSongsIds.includes(track.id) }"
                :style="{ backgroundColor: selectedSongsIds.includes(track.id) ? '#182c25' : '#455b55' }"
                class="track-item"
                @click="toggleSelection(track.id)"
            >
                <img :src="track.picture" alt="Album Cover" class="album-cover" />
                <p class="track-name"><strong>{{ track.title }}</strong></p>
                <p class="artist-name">by {{ track.artist }}</p>
            </div>
            </div>
            <div class="submit-container">
                <button @click="submitSelectedSongs">Submit Selected Songs</button>
            </div>
        </div>

        <div class="logout">
            <button @click="logout">Logout</button>
        </div>
    </div>
</template>

<script>
export default {
    name: 'SpotifyCallback',
    data() {
        return {
            username: '',
            recentTracks: [],
            recentTracksWithBpm: [],
            playlist: null,
            selectedSongsIds: []
        };
    },
    mounted() {
        this.getUser();
        this.getTracksForSession();
    },

    watch: {
        playlist: {
            handler: function (newVal) {
                if (newVal != null && newVal.songs != null) {
                    this.selectedSongsIds = newVal.songs.map(song => song.id);
                }
            },
            deep: true
        }
    },
    computed: {
        currentSelectedSongsDurationInMinutes() {
            const durationInSeconds = this.playlist.songs
                .filter(song => this.selectedSongsIds.includes(song.id))
                .reduce((acc, song) => acc + song.duration, 0);
            if (durationInSeconds === 0) {
                return 0;
            }
            return Math.floor(durationInSeconds / 60);
        },
        neededDurationForSessionInMinutes() {
            const neededDuration = this.playlist.neededDurationInSeconds;
            if (neededDuration === 0) {
                return 0;
            }
            return Math.floor(neededDuration / 60);
        }
    },
    methods: {
        async getUser() {
            console.log('Getting user info');
            try {
                const response = await fetch('http://localhost:5000/spotify/userinfo', {
                    credentials: 'include'
                });

                if (response.ok) {
                    const rawData = await response.text();

                    let data;
                    try {
                        data = JSON.parse(rawData);

                        if (typeof data === 'string') {
                            // For some reason the data is double-encoded
                            data = JSON.parse(data);
                        }
                        
                        console.log('User info:', data);
                        if (data && data.username) {
                            this.username = data.username;
                        } else {
                            console.error('username not found in response data');
                        }
                    } catch (error) {
                        console.error('Error parsing JSON:', error);
                        return;
                    }
                } else {
                    console.error('Failed to fetch user info:', response.statusText);
                }
            } catch (error) {
                console.error('Error during fetch:', error);
            }
        },
        async getTracksForSession() {
            console.log('Getting recently played tracks');
            try {
                const pace = 5;
                const distance = 10;
                const height = 170;
                
                const response = await fetch(`http://localhost:5000/runningsession/playlist?pace=${pace}&distance=${distance}&height=${height}`, {
                    credentials: 'include'
                });

                if (response.ok) {
                    const rawData = await response.text();

                    let data;
                    try {
                        data = JSON.parse(rawData);

                        if (typeof data === 'string') {
                            // For some reason the data is double-encoded
                            data = JSON.parse(data);
                        }
                        console.log('Playlist:', data);
                        this.playlist = data;
                    } catch (error) {
                        console.error('Error parsing JSON:', error);
                        return;
                    }
                } else {
                    console.error('Failed to fetch recently played tracks:', response.statusText);
                }
            } catch (error) {
                console.error('Error during fetch:', error);
            }
        },
        async logout() {
            console.log('Logging out');
            try {
                const response = await fetch('http://localhost:5000/auth/logout', {
                    credentials: 'include'
                });

                if (response.ok) {
                    console.log('Logged out successfully');
                    this.$router.push({ name: 'SpotifyLogin' });
                } else {
                    console.error('Failed to logout:', response.statusText);
                }
            } catch (error) {
                console.error('Error during fetch:', error);
            }
        },
        toggleSelection(songId) {
            const index = this.selectedSongsIds.indexOf(songId);
            if (index === -1) {
                this.selectedSongsIds.push(songId);
            } else {
                this.selectedSongsIds.splice(index, 1);
            }
        },
        submitSelectedSongs() {
            // Send selected songs to the backend
            const request = {
                name: this.playlist.name,
                visible: false,
                colaborative: false,
                songIds: this.selectedSongsIds
            };

            console.log('Submitting selected songs:', request);

            fetch('http://localhost:5000/spotify/playlist', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(request),
                credentials: 'include'
            })
                .then(response => {
                    if (response.ok) {
                        console.log('Selected songs submitted successfully');                        
                    } else {
                        console.error('Failed to submit selected songs:', response.statusText);
                    }
                })
                .catch(error => {
                    console.error('Error during fetch:', error);
                });
        }
    }
};
</script>

<style scoped>
.spotify-container {
    max-width: 900px;
    margin: 0 auto;
    padding: 20px;
    font-family: Arial, sans-serif;
}

.recent-tracks {
    margin-top: 20px;
}

.track-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); /* Responsive grid */
  gap: 20px;
}

.track-item {
  text-align: center;
  border: 1px solid #333;
  border-radius: 10px;
  padding: 10px;
  color: white;
  transition: transform 0.2s ease;
  cursor: pointer;
}

.track-item:hover {
  transform: scale(1.05); /* Slight zoom on hover */
}

.album-cover {
  width: 100px;
  height: 100px;
  object-fit: cover;
  margin-bottom: 10px;
  border-radius: 8px;
}

.track-name {
  font-size: 14px;
  font-weight: bold;
  margin: 5px 0;
  color: white;
}

.artist-name {
  font-size: 12px;
  color: grey;
}

.selected-song {
  border-color: #306844;
  border-width: 3px;
  border-style: solid;
}

progress {
  width: 100%;
  height: 20px;
  border-radius: 10px;
  overflow: hidden;
}

progress::-webkit-progress-bar {
  background-color: #455b55;
  border-radius: 10px;
}

progress::-webkit-progress-value {
  background-color: #182c25;
  border-radius: 10px;
}

.submit-container {
  text-align: center;
  margin-top: 20px;
}

.submit-container button {
  padding: 10px 20px;
  font-size: 16px;
  background-color: #182c25;
  color: white;
  border: none;
  border-radius: 5px;
  cursor: pointer;
}

.submit-container button:hover {
  background-color: #455b55;
}

</style>
