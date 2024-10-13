<template>
    <div>
        <h1 v-if="username">Welcome, {{ username }}!</h1>
        <p v-else>Fetching user info...</p>  <!-- Fallback text if username is empty -->

        <div v-if="recentTracks.length > 0" class="recent-tracks">
            <h2>Recently Played Songs</h2>
            <div class="track-grid">
                <div v-for="(track, index) in recentTracks" :key="index" class="track-item">
                    <img :src="track.picture" alt="Album Cover" class="album-cover" />
                    <p class="track-name"><strong>{{ track.title }}</strong></p>
                    <p class="artist-name">by {{ track.artist }}</p>
                </div>
            </div>
        </div>
        <div v-if="recentTracksWithBpm.length > 0" class="recent-tracks">
            <h2>Recently Played Songs Filtered by BPM</h2>
            <div class="track-grid">
                <div v-for="(track, index) in recentTracksWithBpm" :key="index" class="track-item">
                    <img :src="track.picture" alt="Album Cover" class="album-cover" />
                    <p class="track-name"><strong>{{ track.title }}</strong></p>
                    <p class="artist-name">by {{ track.artist }}</p>
                </div>
            </div>
        </div>

        <div v-if="playlist != null && playlist.songs.length > 0" class="recent-tracks">
            <h2>Playlist tracks: {{ playlist.name  }}</h2>
            <div class="track-grid">
                <div v-for="(track, index) in playlist.songs" :key="index" class="track-item">
                    <img :src="track.picture" alt="Album Cover" class="album-cover" />
                    <p class="track-name"><strong>{{ track.title }}</strong></p>
                    <p class="artist-name">by {{ track.artist }}</p>
                </div>
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
            playlist: null
        };
    },
    mounted() {
        this.getUser();
        // this.getRecentlyPlayed();
        // this.getRecentlyPlayedWithBPM();
        this.getTracksForSession();
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
        async getRecentlyPlayed() {
            console.log('Getting recently played tracks');
            try {
                const limit = 20;
                const response = await fetch('http://localhost:5000/spotify/recently-played?limit=' + limit, {
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

                        this.recentTracks = [...data];
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

        async getRecentlyPlayedWithBPM() {
            console.log('Getting recently played tracks with BPM');
            try {
                const limit = 20;
                const response = await fetch('http://localhost:5000/spotify/recently-played/bpm?limit=' + limit, {
                    credentials: 'include'
                });

                console.log('Full response object:', response);

                if (response.ok) {
                    const rawData = await response.text();

                    console.log('Raw data:', rawData);
                    let data;
                    try {
                        data = JSON.parse(rawData);

                        console.log('Parsed data:', data);

                        if (typeof data === 'string') {
                            // For some reason the data is double-encoded
                            data = JSON.parse(data);
                        }

                        console.log('Recently played tracks:', data);
                        this.recentTracksWithBpm = data;
                        console.log('Recently played tracks with BPM:', this.recentTracksWithBpm);
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
    border: 1px solid #ddd;
    border-radius: 10px;
    padding: 10px;
    background-color: #f9f9f9;
    transition: transform 0.2s ease;
}

.track-item:hover {
    transform: scale(1.05);  /* Slight zoom on hover */
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
}

.artist-name {
    font-size: 12px;
    color: #555;
}
</style>
